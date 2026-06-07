using UnityEngine;

public class StartScreenStuff : MonoBehaviour
{
    public GameObject ConnectionPanel;
    public void StartScreen()
   {
        Debug.Log("Start Button");
        ConnectionPanel.SetActive(true );
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit(); 
    }

    public void CloseLobby()
    {
        ConnectionPanel.SetActive(false);
    }
}
