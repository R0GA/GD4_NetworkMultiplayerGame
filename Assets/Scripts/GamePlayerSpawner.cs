using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GamePlayerSpawner : NetworkBehaviour
{
    [SerializeField] private NetworkObject saboteurPrefab;
    [SerializeField] private NetworkObject seekerPrefab;
    [SerializeField] private Transform slugSpawn;
    [SerializeField] private Transform astroSpawn;
    [SerializeField] private Transform defaultSpawn;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
    }

    private void OnSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode,
                                      List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;

        LobbyNetworkManager lobby = FindObjectOfType<LobbyNetworkManager>();
        if (lobby == null)
        {
            Debug.LogError("LobbyManager not found in game scene!");
            return;
        }

        foreach (ulong clientId in clientsCompleted)
        {
            NetworkObject prefab = null;
            Transform spawnPos = defaultSpawn;

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
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
    }
}