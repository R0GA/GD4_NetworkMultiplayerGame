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

        // Send spawn position only to the owning client
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
        // 1. Disable CC first — it blocks transform.position changes
        if (_cc != null) _cc.enabled = false;

        // 2. Set the position
        transform.position = position;

        // 3. Teleport the NetworkTransform to avoid it interpolating 
        //    from the wrong position back to the right one
        if (_netTransform != null)
            _netTransform.Teleport(position, transform.rotation, transform.localScale);

        // 4. Re-enable CC after position is locked in
        if (_cc != null) _cc.enabled = true;
    }
}