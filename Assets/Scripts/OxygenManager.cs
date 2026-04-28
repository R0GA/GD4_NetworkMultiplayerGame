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
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );


    public UnityEvent<float, float> OnOxygenChanged;  
    public UnityEvent<bool> OnLowOxygenChanged;    

    public UnityEvent OnDeath = new UnityEvent();

    private bool wasLowOxygen = false;
    private bool isDead = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentOxygen.Value = maxOxygen;
        }

        currentOxygen.OnValueChanged += OnOxygenValueChanged;
        OnOxygenValueChanged(0, currentOxygen.Value);
    }

    private void OnDisable()
    {
        currentOxygen.OnValueChanged -= OnOxygenValueChanged;
    }

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

    private void OnOxygenValueChanged(float previous, float current)
    {
        OnOxygenChanged?.Invoke(current, maxOxygen);

        bool isLow = current <= lowOxygenThreshold;
        if (isLow != wasLowOxygen)
        {
            wasLowOxygen = isLow;
            OnLowOxygenChanged?.Invoke(isLow);
        }

        if (current <= 0f && !isDead)
        {
            isDead = true;
            OnDeath?.Invoke();
        }
    }

    public void RefillOxygen(float amount)
    {
        if (!IsServer) return;
        currentOxygen.Value = Mathf.Min(currentOxygen.Value + amount, maxOxygen);
    }

    public float CurrentOxygen => currentOxygen.Value;
    public float MaxOxygen => maxOxygen;
    public bool IsLowOxygen => currentOxygen.Value <= lowOxygenThreshold;
    public bool IsDead => currentOxygen.Value <= 0f;
}