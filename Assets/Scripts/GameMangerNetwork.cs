using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManagerNetwork : NetworkBehaviour
{
    public static GameManagerNetwork Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void NotifyClientsGameEnd(string sceneName)
    {
        if (!IsServer) return;
        Debug.Log("[GameManagerNetwork] Sending end scene RPC to clients");
        StoreEndSceneClientRpc(sceneName);
    }

    [ClientRpc]
    private void StoreEndSceneClientRpc(string sceneName)
    {
        // Skip host — GameManager handles host directly
        if (IsServer) return;

        Debug.Log($"[GameManagerNetwork] ✅ Client RPC received, loading: {sceneName}");
        StartCoroutine(ClientShutdownAndLoad(sceneName));
    }

    private IEnumerator ClientShutdownAndLoad(string sceneName)
    {
        Debug.Log("[GameManagerNetwork] Client starting shutdown...");

        // Give the server a moment to finish sending before we pull the plug
        yield return new WaitForSecondsRealtime(0.1f);

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("[GameManagerNetwork] Client NetworkManager shutdown called");
        }

        // Wait for shutdown to complete
        float timeout = 3f;
        float elapsed = 0f;
        while (NetworkManager.Singleton != null &&
               NetworkManager.Singleton.IsListening &&
               elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Debug.Log($"[GameManagerNetwork] Client shutdown complete (elapsed: {elapsed:F2}s), destroying NetworkManager");

        if (NetworkManager.Singleton != null)
            Destroy(NetworkManager.Singleton.gameObject);

        // One more frame to let Destroy process
        yield return null;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log($"[GameManagerNetwork] Client loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}