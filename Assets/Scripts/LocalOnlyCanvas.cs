using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LocalOnlyCanvas : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private GraphicRaycaster raycaster;

    private void Start()
    {
        // Hide everything immediately, wait for local player
        canvas.enabled = false;
        raycaster.enabled = false;

        // Poll until the local player is spawned
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        // Only react to the local client connecting
        if (clientId != NetworkManager.Singleton.LocalClientId) return;

        canvas.enabled = true;
        raycaster.enabled = true;

        // Unsubscribe — we only need this once
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }
}