using UnityEngine;

public class SeeEngineCamera : MonoBehaviour
{
    public GameObject engineCamCanvas;

    [SerializeField] private SlugPlayer slugPlayer;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Slug"))
        {
            engineCamCanvas.SetActive(true);
            slugPlayer.SetUIMode(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Slug"))
        {
            engineCamCanvas.SetActive(false);
            slugPlayer.SetUIMode(false);
        }
    }
}