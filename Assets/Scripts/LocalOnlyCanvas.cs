using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LocalOnlyCanvas : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private GraphicRaycaster raycaster;

    private void Start()
    {
        canvas.enabled = false;
        raycaster.enabled = false;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId != NetworkManager.Singleton.LocalClientId) return;

        canvas.enabled = true;
        raycaster.enabled = true;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }
}