using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GamePlayerSpawner : NetworkBehaviour
{
    [SerializeField] private NetworkObject saboteurPrefab;
    [SerializeField] private NetworkObject seekerPrefab;
    [SerializeField] private Transform slugSpawn;
    [SerializeField] private Transform astroSpawn;
    private Transform spawnPos;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // We already know the lobby manager survived, find it
        LobbyNetworkManager lobby = FindObjectOfType<LobbyNetworkManager>();
        if (lobby == null)
        {
            Debug.LogError("LobbyManager not found in game scene!");
            return;
        }

        // Scene is already loaded when this NetworkObject spawns.
        // Spawn each connected client’s character.
        var clients = NetworkManager.Singleton.ConnectedClients;
        foreach (var kvp in clients)
        {
            ulong clientId = kvp.Key;
            NetworkObject prefab = null;

            if (clientId == lobby.SaboteurClientId.Value)
            {
                prefab = saboteurPrefab;
                spawnPos = slugSpawn;
            }

            else if (clientId == lobby.SeekerClientId.Value)
            {
                prefab = seekerPrefab;
                spawnPos = astroSpawn;
            }

                if (prefab != null)
            {
                var playerObj = Instantiate(prefab, spawnPos.position, Quaternion.identity);
                playerObj.SpawnAsPlayerObject(clientId);
            }
        }

        // (Optional) destroy the lobby manager now that it’s done
        //Destroy(lobby.gameObject);
    }
}