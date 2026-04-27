using UnityEngine;

public class SeeReactor : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject reactorCamCanvas;
    [SerializeField] private SlugPlayer slugPlayer;
    [SerializeField] private GameObject slugman;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("collided" + other);
        if (other.CompareTag("Slug"))
        {
            reactorCamCanvas.SetActive(true);
            other.GetComponent<SlugPlayer>().SetUIMode(true);
            slugman = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Slug"))
        {
            reactorCamCanvas.SetActive(false);
            other.GetComponent<SlugPlayer>().SetUIMode(false);
        }
    }

    public void Close()
    {
        reactorCamCanvas.SetActive(false);
        slugman.GetComponent<SlugPlayer>().SetUIMode(false);
    }
}
