using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class SeeReactor : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject reactorCamCanvas;
    [SerializeField] private SlugPlayer slugPlayer;
    [SerializeField] private GameObject slugman;
    [SerializeField] private Canvas myCanvas;
    [SerializeField] private GraphicRaycaster raycaster;

    // Called when a player interacts (e.g. press E, enter trigger, etc.)
    public void Interact(NetworkBehaviour interactingPlayer)
    {
        // Only open the canvas for the player who actually interacted
        if (!interactingPlayer.IsOwner) return;

        myCanvas.enabled = true;
        raycaster.enabled = true;
        myCanvas.worldCamera = slugman.GetComponentInChildren<Camera>();
        Debug.Log($"Interacted with reactor camera{myCanvas} {raycaster} {myCanvas.worldCamera}");
    }
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("collided" + other);
        if (other.CompareTag("Slug"))
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
            other.GetComponent<SlugPlayer>().SetUIMode(false);
        }
    }

    public void Close()
    {
        myCanvas.enabled = false;
        raycaster.enabled = false;
        slugman.GetComponent<SlugPlayer>().SetUIMode(false);
    }
}
