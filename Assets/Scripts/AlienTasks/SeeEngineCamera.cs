using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class SeeEngineCamera : MonoBehaviour
{
    public GameObject engineCamCanvas;
    [SerializeField] private SlugPlayer slugPlayer;
    [SerializeField] private GameObject slugman;
    [SerializeField] private Canvas myCanvas;
    [SerializeField] private GraphicRaycaster raycaster;
    public bool taskCompleted = false;

    public void Interact(NetworkBehaviour interactingPlayer)
    {
        if (!interactingPlayer.IsOwner) return;

        myCanvas.enabled = true;
        raycaster.enabled = true;
        myCanvas.worldCamera = slugman.GetComponentInChildren<Camera>();
    }
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("collided" + other);
        if (other.CompareTag("Slug") && !taskCompleted)
        {
            other.GetComponent<SlugPlayer>().SetUIMode(true);
            slugman = other.gameObject;
            Interact(other.GetComponent<NetworkBehaviour>());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Slug"))
        {
            Close();
            //other.GetComponent<SlugPlayer>().SetUIMode(false);
        }
    }

    public void Close()
    {
        myCanvas.enabled = false;
        raycaster.enabled = false;
        slugman.GetComponent<SlugPlayer>().SetUIMode(false);
    }
}