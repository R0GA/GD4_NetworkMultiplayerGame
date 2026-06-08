using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class OxygenManager : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float maxOxygen = 100f;
    [SerializeField] private float drainRate = 5f;
    [SerializeField] private float lowOxygenThreshold = 25f;

    private NetworkVariable<float> currentOxygen = new NetworkVariable<float>(
        100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Same pattern as NetworkHealth — server-authoritative, syncs to all peers
    public NetworkVariable<bool> IsDead = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public UnityEvent<float, float> OnOxygenChanged;
    public UnityEvent<bool> OnLowOxygenChanged;
    public UnityEvent OnDeath = new UnityEvent();

    private bool wasLowOxygen = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            currentOxygen.Value = maxOxygen;

        currentOxygen.OnValueChanged += OnOxygenValueChanged;
        IsDead.OnValueChanged += OnIsDeadChanged;

        // Fire initial UI update without triggering death logic
        OnOxygenChanged?.Invoke(currentOxygen.Value, maxOxygen);
    }

    public override void OnNetworkDespawn()
    {
        currentOxygen.OnValueChanged -= OnOxygenValueChanged;
        IsDead.OnValueChanged -= OnIsDeadChanged;
    }

    // Remove OnDisable — OnNetworkDespawn is the correct NGO cleanup hook

    private void Update()
    {
        if (!IsServer || IsDead.Value) return;

        currentOxygen.Value -= drainRate * Time.deltaTime;
        if (currentOxygen.Value < 0f)
            currentOxygen.Value = 0f;
    }

    private void OnOxygenValueChanged(float previous, float current)
    {
        OnOxygenChanged?.Invoke(current, maxOxygen);

        bool isLow = current <= lowOxygenThreshold;
        if (isLow != wasLowOxygen)
        {
            wasLowOxygen = isLow;
            OnLowOxygenChanged?.Invoke(isLow);
        }

        // Server sets the flag; NetworkVariable propagates it to all peers
        if (IsServer && current <= 0f && !IsDead.Value)
            IsDead.Value = true;
    }

    private void OnIsDeadChanged(bool previous, bool current)
    {
        if (current)
        {
            Debug.Log($"[OxygenManager] Death confirmed on {(IsServer ? "server" : "client")}");
            OnDeath?.Invoke();
        }
    }

    public void RefillOxygen(float amount)
    {
        if (!IsServer) return;
        currentOxygen.Value = Mathf.Min(currentOxygen.Value + amount, maxOxygen);
    }

    public void DrainOxygen(float amount)
    {
        if (!IsServer) return;
        currentOxygen.Value = Mathf.Max(currentOxygen.Value - amount, 0f);
    }

    public float CurrentOxygen => currentOxygen.Value;
    public float MaxOxygen => maxOxygen;
    public bool IsLowOxygen => currentOxygen.Value <= lowOxygenThreshold;
}