using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class EndSceneCleanup : MonoBehaviour
{
    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        // The match is over: wipe the persistent GameManager so the next match
        // starts from a clean state (state = Active, no stale listeners).
        if (GameManager.Instance != null)
            GameManager.Instance.ResetForNewMatch(matchOver: true);

        StartCoroutine(ShutdownNetwork());
    }

    private IEnumerator ShutdownNetwork()
    {
        // Hold our own reference: don't rely on the Singleton staying valid mid-shutdown.
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null) yield break;

        Debug.Log("[EndSceneCleanup] Shutting down NetworkManager");
        nm.Shutdown();

        yield return new WaitUntil(() => nm == null || !nm.IsListening);
        yield return null; // let NGO finish despawning its objects

        if (nm != null)
        {
            Debug.Log("[EndSceneCleanup] Destroying NetworkManager");
            Destroy(nm.gameObject);
        }

        // Safety net: make sure no DontDestroyOnLoad lobby object survives into the next match.
        foreach (var lobby in FindObjectsByType<LobbyNetworkManager>(FindObjectsSortMode.None))
            Destroy(lobby.gameObject);

        Debug.Log("[EndSceneCleanup] Network cleaned up, scene ready");
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
