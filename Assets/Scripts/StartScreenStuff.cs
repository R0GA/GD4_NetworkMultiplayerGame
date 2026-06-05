using UnityEngine;

public class StartScreenStuff : MonoBehaviour
{
    public void StartScreen()
   {
        Debug.Log("Start Button");
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
       
    }
}
