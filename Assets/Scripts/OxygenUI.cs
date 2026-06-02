using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class OxygenUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image radialFillImage;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float flashSpeed = 3f;
    [SerializeField] private float lowOxygenFlashAlpha = 0.3f;

    private OxygenManager oxygenManager;
    private bool isLow = false;

    private void Start()
    {
        oxygenManager = FindLocalSpacemanOxygenManager();

        if (oxygenManager != null)
        {
            oxygenManager.OnOxygenChanged.AddListener(UpdateFill);
            oxygenManager.OnLowOxygenChanged.AddListener(SetLowOxygen);

            UpdateFill(oxygenManager.CurrentOxygen, oxygenManager.MaxOxygen);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isLow) return;

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

    private OxygenManager FindLocalSpacemanOxygenManager()
    {
        foreach (var netObj in FindObjectsOfType<NetworkObject>())
        {
            if (netObj.IsOwner && netObj.TryGetComponent<OxygenManager>(out var om))
                return om;
        }
        return null;
    }
}