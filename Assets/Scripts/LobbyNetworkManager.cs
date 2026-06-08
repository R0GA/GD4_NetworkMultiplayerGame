using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages role selection and the ready-up flow in the lobby scene.
///
/// Fixes vs original:
///   • Removed the private `readyClients` NetworkList — it was a duplicate of
///     the public `ReadyClients` that caused a two-list desync.
///   • SetReadyServerRpc is removed (it was also a duplicate of ToggleReadyServerRpc
///     but wrote only to the private list, so it never showed as ready on remotes).
///   • Role-check in ToggleReadyServerRpc now uses != UNASSIGNED (was != 0,
///     which is a valid client id on the host).
/// </summary>
public class LobbyNetworkManager : NetworkBehaviour
{
    [SerializeField] private string gameSceneName = "GameScene";

    public const ulong UNASSIGNED = ulong.MaxValue;

    public NetworkVariable<ulong> SaboteurClientId = new(UNASSIGNED);
    public NetworkVariable<ulong> SeekerClientId = new(UNASSIGNED);

    /// <summary>Client ids that have pressed Ready (and have a role).</summary>
    public NetworkList<ulong> ReadyClients = new();

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        ReadyClients.OnListChanged += OnReadyListChanged;
        SaboteurClientId.OnValueChanged += OnRoleChanged;
        SeekerClientId.OnValueChanged += OnRoleChanged;
    }

    public override void OnNetworkDespawn()
    {
        ReadyClients.OnListChanged -= OnReadyListChanged;
        SaboteurClientId.OnValueChanged -= OnRoleChanged;
        SeekerClientId.OnValueChanged -= OnRoleChanged;
    }

    private void OnDisable()
    {
        if (IsServer && NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    // -------------------------------------------------------------------------
    // Role selection
    // -------------------------------------------------------------------------

    [ServerRpc(RequireOwnership = false)]
    public void RequestRoleServerRpc(RoleType role, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        // Clear any role the client already held.
        if (SaboteurClientId.Value == clientId) SaboteurClientId.Value = UNASSIGNED;
        if (SeekerClientId.Value == clientId) SeekerClientId.Value = UNASSIGNED;

        // Selecting a new role un-readies the client.
        ReadyClients.Remove(clientId);

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
    }

    [ServerRpc(RequireOwnership = false)]
    public void ClearMyRoleServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (SaboteurClientId.Value == clientId) SaboteurClientId.Value = UNASSIGNED;
        if (SeekerClientId.Value == clientId) SeekerClientId.Value = UNASSIGNED;
        ReadyClients.Remove(clientId);
    }

    // -------------------------------------------------------------------------
    // Ready toggle
    // -------------------------------------------------------------------------

    [ServerRpc(RequireOwnership = false)]
    public void ToggleReadyServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        // Only clients who have picked a role may ready up.
        bool hasRole = SaboteurClientId.Value == clientId || SeekerClientId.Value == clientId;
        if (!hasRole) return;

        if (ReadyClients.Contains(clientId))
            ReadyClients.Remove(clientId);
        else
            ReadyClients.Add(clientId);

        TryStartGame();
    }

    // -------------------------------------------------------------------------
    // Disconnect handling
    // -------------------------------------------------------------------------

    private void OnClientDisconnected(ulong clientId)
    {
        if (SaboteurClientId.Value == clientId) SaboteurClientId.Value = UNASSIGNED;
        if (SeekerClientId.Value == clientId) SeekerClientId.Value = UNASSIGNED;
        ReadyClients.Remove(clientId);
    }

    // -------------------------------------------------------------------------
    // Game start
    // -------------------------------------------------------------------------

    private void TryStartGame()
    {
        if (ReadyClients.Count < 2) return;
        if (SaboteurClientId.Value == UNASSIGNED) return;
        if (SeekerClientId.Value == UNASSIGNED) return;
        if (SaboteurClientId.Value == SeekerClientId.Value) return;

        StartGame();
    }

    private void StartGame()
    {
        DontDestroyOnLoad(gameObject);
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    // -------------------------------------------------------------------------
    // UI hooks — override / subscribe to these in your UI scripts
    // -------------------------------------------------------------------------

    private void OnReadyListChanged(NetworkListEvent<ulong> changeEvent) { /* refresh ready UI */ }
    private void OnRoleChanged(ulong previous, ulong current) { /* refresh role UI  */ }
}

public enum RoleType { Saboteur, Seeker }