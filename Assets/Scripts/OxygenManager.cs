using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class OxygenManager : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float maxOxygen = 100f;
    [SerializeField] private float drainRate = 5f;         // units per second
    [SerializeField] private float lowOxygenThreshold = 25f;

    // NetworkVariable automatically syncs from server to clients
    private NetworkVariable<float> currentOxygen = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Events that local UI can subscribe to
    public UnityEvent<float, float> OnOxygenChanged;   // (current, max)
    public UnityEvent<bool> OnLowOxygenChanged;        // true when below threshold

    private bool wasLowOxygen = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentOxygen.Value = maxOxygen;
        }

        // Subscribe to value changes so UI updates on all clients
        currentOxygen.OnValueChanged += OnOxygenValueChanged;
        // Initial update for late-joining clients
        OnOxygenValueChanged(0, currentOxygen.Value);
    }

    private void OnDisable()
    {
        currentOxygen.OnValueChanged -= OnOxygenValueChanged;
    }

    // Server only: drain oxygen each frame
    private void Update()
    {
        if (!IsServer) return;

        if (currentOxygen.Value > 0f)
        {
            currentOxygen.Value -= drainRate * Time.deltaTime;
            if (currentOxygen.Value < 0f)
                currentOxygen.Value = 0f;
        }
    }

    // Called on every client when the NetworkVariable changes
    private void OnOxygenValueChanged(float previous, float current)
    {
        OnOxygenChanged?.Invoke(current, maxOxygen);

        bool isLow = current <= lowOxygenThreshold;
        if (isLow != wasLowOxygen)
        {
            wasLowOxygen = isLow;
            OnLowOxygenChanged?.Invoke(isLow);
        }
    }

    // Call this from a ServerRpc to refill oxygen (or directly on server)
    public void RefillOxygen(float amount)
    {
        if (!IsServer) return;
        currentOxygen.Value = Mathf.Min(currentOxygen.Value + amount, maxOxygen);
    }

    // Expose values for other scripts (e.g., to check death)
    public float CurrentOxygen => currentOxygen.Value;
    public float MaxOxygen => maxOxygen;
    public bool IsLowOxygen => currentOxygen.Value <= lowOxygenThreshold;
    public bool IsDead => currentOxygen.Value <= 0f;
}