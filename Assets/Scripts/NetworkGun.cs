using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkGun : NetworkBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private NetworkObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 20f;

    private PlayerInput pi;
    private InputAction shootAction;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        pi = GetComponent<PlayerInput>();
        shootAction = pi.actions["Shoot"];
        shootAction.Enable();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (shootAction.WasPressedThisFrame())
            ShootServerRPC(firePoint.position, firePoint.forward);
    }

    [ServerRpc]
    private void ShootServerRPC(Vector3 pos, Vector3 forward)
    {
        var proj = Instantiate(projectilePrefab, pos, Quaternion.LookRotation(forward));
        proj.Spawn();
        var rb = proj.GetComponent<Rigidbody>();
        rb.linearVelocity = forward * projectileSpeed;
    }
}
