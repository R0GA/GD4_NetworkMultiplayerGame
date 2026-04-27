using UnityEngine;

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
}
