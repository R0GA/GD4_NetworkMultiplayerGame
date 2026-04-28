using UnityEngine;

public class BackToMenuScript : MonoBehaviour
{
    public void BackToMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("JoinMenu");
    }
}
