using Unity.Netcode;
using UnityEngine;

public class OxygenRefillStation : NetworkBehaviour
{
    [SerializeField] private float refillAmount = 30f;
    [SerializeField] private float cooldown = 5f;   
    private float lastRefillTime;
    private AudioSource audioSource;

    private void OnTriggerStay(Collider other)
    {
        if (!IsServer) return; 

        if (other.TryGetComponent<OxygenManager>(out var oxygenManager))
        {
            if (Time.time - lastRefillTime < cooldown)
                return;

            oxygenManager.RefillOxygen(refillAmount);
            lastRefillTime = Time.time;

            OnRefillClientRpc();
        }
    }

    [ClientRpc]
    private void OnRefillClientRpc()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.Play();
    }
}