using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Controls : MonoBehaviour
{
    public GameObject controls;

    public void OpenControls()
    {
        controls.SetActive(true);
    }
    public void CloseControls()
    {
        controls.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene(0);
    }
}
