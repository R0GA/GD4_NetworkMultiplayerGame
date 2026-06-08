using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class NetworkHealth : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 100;

    public NetworkVariable<int> Health = new NetworkVariable<int>(
        100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Server-authoritative death flag — syncs to all peers automatically
    public NetworkVariable<bool> IsDead = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public UnityEvent OnDeath = new UnityEvent();

    public override void OnNetworkSpawn()
    {
        IsDead.OnValueChanged += OnIsDeadChanged;

        // Cover the case where IsDead is already true on spawn (late join)
        if (IsDead.Value)
            OnDeath?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        IsDead.OnValueChanged -= OnIsDeadChanged;
    }

    private void OnIsDeadChanged(bool previous, bool current)
    {
        if (current)
        {
            Debug.Log($"[NetworkHealth] Death confirmed on {(IsServer ? "server" : "client")}");
            OnDeath?.Invoke();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TakeDamageServerRpc(int amount)
    {
        // Server is the single source of truth — ignore damage if already dead
        if (IsDead.Value) return;

        Health.Value = Mathf.Max(0, Health.Value - amount);

        if (Health.Value <= 0)
            IsDead.Value = true; // triggers OnIsDeadChanged on ALL peers
    }

    public float Health01 => maxHealth == 0 ? 0f : (float)Health.Value / maxHealth;
}