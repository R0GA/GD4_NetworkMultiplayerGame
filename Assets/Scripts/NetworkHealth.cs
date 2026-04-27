using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class NetworkHealth : NetworkBehaviour
{
    [SerializeField] private int maxHealth;

    public NetworkVariable<int> Health = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Event triggered when health reaches 0
    public UnityEvent OnDeath = new UnityEvent();

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TakeDamageServerRpc(int amount)
    {
        Health.Value -= amount;
        if (Health.Value <= 0)
        {
            Health.Value = 0;
            OnDeath?.Invoke();
        }
    }
    public float Health01
    {
        get
        {
            if(maxHealth == 0) return 0f;
            else return (float)Health.Value / maxHealth;
        }
    }
}
