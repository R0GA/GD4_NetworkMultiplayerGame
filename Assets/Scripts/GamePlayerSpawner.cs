using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Server-side spawner. After spawning each player prefab it tells the
/// GameManager which role each NetworkObject represents so win-condition
/// listeners can be attached without polling.
/// </summary>
public class GamePlayerSpawner : NetworkBehaviour
{
    [SerializeField] private NetworkObject saboteurPrefab;   // SlugPlayer
    [SerializeField] private NetworkObject seekerPrefab;     // NetworkFPSPlayer
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

        var lobby = FindObjectOfType<LobbyNetworkManager>();
        if (lobby == null)
        {
            Debug.LogError("[GamePlayerSpawner] LobbyNetworkManager not found in game scene!");
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

            if (prefab == null) continue;

            var playerObj = Instantiate(prefab, spawnPos != null ? spawnPos.position : Vector3.zero, Quaternion.identity);
            playerObj.SpawnAsPlayerObject(clientId);

            // Registration with GameManager happens inside each player's own
            // OnNetworkSpawn (see SlugPlayer / NetworkFPSPlayer), so we don't
            // need to touch GameManager here.
        }
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
    }
}