using UnityEngine;

public class SeeEngineCamera : MonoBehaviour
{
    public GameObject engineCamCanvas;
    [SerializeField] private SlugPlayer slugPlayer;
   [SerializeField] private GameObject slugman;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("collided" + other);
        if (other.CompareTag("Slug"))
        {
            engineCamCanvas.SetActive(true);
            other.GetComponent<SlugPlayer>().SetUIMode(true);
            slugman = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Slug"))
        {
            engineCamCanvas.SetActive(false);
            other.GetComponent<SlugPlayer>().SetUIMode(false);
        }
    }

    public void Close()
    {
        engineCamCanvas.SetActive(false);
        slugman.GetComponent<SlugPlayer>().SetUIMode(false);
    }
}