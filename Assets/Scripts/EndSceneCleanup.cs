using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class EndSceneCleanup : MonoBehaviour
{
    private void Start()
    {
        // Fix cursor immediately so UI is usable
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCoroutine(ShutdownNetwork());
    }

    private IEnumerator ShutdownNetwork()
    {
        if (NetworkManager.Singleton == null) yield break;

        Debug.Log("[EndSceneCleanup] Shutting down NetworkManager");
        NetworkManager.Singleton.Shutdown();

        // Wait for shutdown to fully complete
        yield return new WaitUntil(() =>
            NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsListening
        );

        if (NetworkManager.Singleton != null)
        {
            Debug.Log("[EndSceneCleanup] Destroying NetworkManager");
            Destroy(NetworkManager.Singleton.gameObject);
        }

        Debug.Log("[EndSceneCleanup] Network cleaned up, scene ready");
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}