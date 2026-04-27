using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class OxygenUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image radialFillImage;
    [SerializeField] private CanvasGroup canvasGroup;   // For easy flashing
    [SerializeField] private float flashSpeed = 3f;
    [SerializeField] private float lowOxygenFlashAlpha = 0.3f;

    private OxygenManager oxygenManager;
    private bool isLow = false;

    private void Start()
    {
        // Find the local player’s OxygenManager (assuming only Spaceman has it)
        // In a real project you’d use a player registry or GetComponent on the player prefab.
        oxygenManager = FindLocalSpacemanOxygenManager();

        if (oxygenManager != null)
        {
            oxygenManager.OnOxygenChanged.AddListener(UpdateFill);
            oxygenManager.OnLowOxygenChanged.AddListener(SetLowOxygen);

            // Set initial fill
            UpdateFill(oxygenManager.CurrentOxygen, oxygenManager.MaxOxygen);
        }
        else
        {
            // If no oxygen manager (e.g., alien), disable the UI
            gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isLow) return;

        // Flashing effect: oscillate alpha
        float alpha = Mathf.PingPong(Time.time * flashSpeed, 1f - lowOxygenFlashAlpha) + lowOxygenFlashAlpha;
        canvasGroup.alpha = alpha;
    }

    private void UpdateFill(float current, float max)
    {
        radialFillImage.fillAmount = current / max;
    }

    private void SetLowOxygen(bool low)
    {
        isLow = low;
        if (!low)
            canvasGroup.alpha = 1f;
    }

    private void OnDestroy()
    {
        if (oxygenManager != null)
        {
            oxygenManager.OnOxygenChanged.RemoveListener(UpdateFill);
            oxygenManager.OnLowOxygenChanged.RemoveListener(SetLowOxygen);
        }
    }

    // Replace with your own method to get the local Spaceman’s OxygenManager.
    private OxygenManager FindLocalSpacemanOxygenManager()
    {
        // Example: Find any player prefab that is owned by this client and has the component.
        // For a simple two‑player game you can use FindObjectOfType and check ownership.
        foreach (var netObj in FindObjectsOfType<NetworkObject>())
        {
            if (netObj.IsOwner && netObj.TryGetComponent<OxygenManager>(out var om))
                return om;
        }
        return null;
    }
}