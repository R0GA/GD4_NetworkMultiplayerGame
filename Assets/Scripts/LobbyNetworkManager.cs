using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyNetworkManager : NetworkBehaviour
{
    [SerializeField] private string gameSceneName = "GameScene";

    private NetworkList<ulong> readyClients = new();

    public const ulong UNASSIGNED = ulong.MaxValue;

    public NetworkVariable<ulong> SaboteurClientId = new NetworkVariable<ulong>(UNASSIGNED);
    public NetworkVariable<ulong> SeekerClientId = new NetworkVariable<ulong>(UNASSIGNED);

    public NetworkList<ulong> ReadyClients = new NetworkList<ulong>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        readyClients.OnListChanged += OnReadyListChanged;
        SaboteurClientId.OnValueChanged += OnRoleChanged;
        SeekerClientId.OnValueChanged += OnRoleChanged;
    }

    private void OnDisable()
    {
        if (IsServer && NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }
    [ServerRpc(RequireOwnership = false)]
    public void RequestRoleServerRpc(RoleType role, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        // Clear any existing role for this client
        if (SaboteurClientId.Value == clientId) SaboteurClientId.Value = UNASSIGNED;
        if (SeekerClientId.Value == clientId) SeekerClientId.Value = UNASSIGNED;

        // Assign new role if free
        switch (role)
        {
            case RoleType.Saboteur:
                if (SaboteurClientId.Value == UNASSIGNED)
                    SaboteurClientId.Value = clientId;
                break;
            case RoleType.Seeker:
                if (SeekerClientId.Value == UNASSIGNED)
                    SeekerClientId.Value = clientId;
                break;
        }

        // Force un‑ready whenever role changes
        ReadyClients.Remove(clientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ToggleReadyServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        // Only allow ready/unready if the client has a role
        if (SaboteurClientId.Value == clientId || SeekerClientId.Value == clientId)
        {
            if (ReadyClients.Contains(clientId))
                ReadyClients.Remove(clientId);
            else
                ReadyClients.Add(clientId);
        }

        // Check if we can start
        if (ReadyClients.Count >= 2 &&
            SaboteurClientId.Value != UNASSIGNED &&
            SeekerClientId.Value != UNASSIGNED &&
            SaboteurClientId.Value != SeekerClientId.Value)
        {
            StartGame();
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (SaboteurClientId.Value == clientId) SaboteurClientId.Value = UNASSIGNED;
        if (SeekerClientId.Value == clientId) SeekerClientId.Value = UNASSIGNED;
        ReadyClients.Remove(clientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetReadyServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        // Only allow ready if the client has a role
        if (SaboteurClientId.Value == clientId || SeekerClientId.Value == clientId)
        {
            if (!readyClients.Contains(clientId))
                readyClients.Add(clientId);

            // Check if all players (2) are ready and roles are filled
            if (readyClients.Count >= 2 &&
                SaboteurClientId.Value != 0 &&
                SeekerClientId.Value != 0 &&
                SaboteurClientId.Value != SeekerClientId.Value)
            {
                StartGame();
            }
        }
    }

    private void StartGame()
    {
        // Ensure this object survives the scene load so we can spawn players later
        DontDestroyOnLoad(gameObject);
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    public override void OnNetworkDespawn()
    {
        // Clean up callbacks if ever despawned
        readyClients.OnListChanged -= OnReadyListChanged;
        SaboteurClientId.OnValueChanged -= OnRoleChanged;
        SeekerClientId.OnValueChanged -= OnRoleChanged;
    }
    [ServerRpc(RequireOwnership = false)]
    public void ClearMyRoleServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (SaboteurClientId.Value == clientId) SaboteurClientId.Value = UNASSIGNED;
        if (SeekerClientId.Value == clientId) SeekerClientId.Value = UNASSIGNED;
        ReadyClients.Remove(clientId);   // auto‑unready when clearing
    }

    // Optional: react to changes locally (e.g., update UI)
    private void OnReadyListChanged(NetworkListEvent<ulong> changeEvent) { /* update UI */ }
    private void OnRoleChanged(ulong previous, ulong current) { /* update UI */ }
}

public enum RoleType { Saboteur, Seeker }