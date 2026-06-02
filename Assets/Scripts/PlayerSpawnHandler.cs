using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerSpawnHandler : NetworkBehaviour
{
    private CharacterController _cc;
    private NetworkTransform _netTransform;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _netTransform = GetComponent<NetworkTransform>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        TeleportOwnerClientRpc(
            transform.position,
            new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { OwnerClientId }
                }
            }
        );
    }

    [ClientRpc]
    private void TeleportOwnerClientRpc(Vector3 spawnPosition, ClientRpcParams _ = default)
    {
        ForcePosition(spawnPosition);
    }

    private void ForcePosition(Vector3 position)
    {
        if (_cc != null) _cc.enabled = false;

        transform.position = position;

        if (_netTransform != null)
            _netTransform.Teleport(position, transform.rotation, transform.localScale);

        if (_cc != null) _cc.enabled = true;
    }
}