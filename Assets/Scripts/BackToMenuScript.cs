using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToMenuScript : MonoBehaviour
{
    [SerializeField] private string menuSceneName = "NewStart";

    private bool loading;

    public void BackToMenu()
    {
        if (loading) return;
        loading = true;
        StartCoroutine(BackToMenuRoutine());
    }

    private IEnumerator BackToMenuRoutine()
    {
        // EndSceneCleanup normally does this already. Doing it again here is harmless
        // and guarantees the old (DontDestroyOnLoad) NetworkManager is gone before the
        // menu scene brings in its own, which would otherwise be rejected as a duplicate.
        NetworkManager nm = NetworkManager.Singleton;
        if (nm != null)
        {
            if (nm.IsListening)
            {
                nm.Shutdown();
                yield return new WaitUntil(() => nm == null || !nm.IsListening);
                yield return null;
            }

            if (nm != null)
            {
                Destroy(nm.gameObject);
                yield return null; // Destroy is deferred to end of frame
            }
        }

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(menuSceneName);
    }
}
