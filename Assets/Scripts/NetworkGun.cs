using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkGun : NetworkBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private NetworkObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 20f;

    [Header("Aiming")]
    [SerializeField] private float targetDistance = 50f; // How far ahead we aim toward crosshair

    private PlayerInput pi;
    private InputAction shootAction;
    private NetworkFPSPlayer player;
    private Camera playerCamera;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        player = GetComponentInParent<NetworkFPSPlayer>();
        if (player != null)
            playerCamera = player.PlayerCamera;

        if (playerCamera == null)
            Debug.LogWarning("NetworkGun: No camera found for aiming.", this);

        pi = GetComponent<PlayerInput>();
        shootAction = pi.actions["Shoot"];
        shootAction.Enable();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (shootAction.WasPressedThisFrame())
        {
            // Calculate the direction from firePoint to the crosshair target point
            Vector3 direction = GetAimDirection();
            ShootServerRPC(firePoint.position, direction);
        }
    }

    private Vector3 GetAimDirection()
    {
        if (playerCamera == null)
            return firePoint.forward; // fallback

        // The point in world space that the camera is looking at (at targetDistance)
        Vector3 targetPoint = playerCamera.transform.position + playerCamera.transform.forward * targetDistance;

        // Direction from the gun's fire point toward that target point
        Vector3 direction = (targetPoint - firePoint.position).normalized;

        return direction;
    }

    [ServerRpc]
    private void ShootServerRPC(Vector3 pos, Vector3 direction)
    {
        var proj = Instantiate(projectilePrefab, pos, Quaternion.LookRotation(direction));
        proj.Spawn();
        var rb = proj.GetComponent<Rigidbody>();
        rb.linearVelocity = direction * projectileSpeed;
    }
}