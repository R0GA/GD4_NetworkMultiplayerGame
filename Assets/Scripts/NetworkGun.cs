using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkGun : NetworkBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private NetworkObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 20f;
    [SerializeField] private ParticleSystem repulsorBlast;

    [Header("Aiming")]
    [SerializeField] private float targetDistance = 50f;
    [SerializeField] private LayerMask aimMask = ~0;

    [Header("Shooting Cost")]
    [SerializeField] private float oxygenCostPerShot = 2f;

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
            // Step 1: raycast from camera centre to find the world point the crosshair is on
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hit, targetDistance, aimMask)
                ? hit.point
                : ray.GetPoint(targetDistance);

            // Step 2: bullet spawns at firePoint, but aims AT the camera's target point
            // This makes the bullet path converge on the crosshair regardless of barrel offset
            Vector3 spawnPos = firePoint != null ? firePoint.position : playerCamera.transform.position;
            Vector3 direction = (targetPoint - spawnPos).normalized;

            PlayVFXClientRpc();
            ShootServerRpc(spawnPos, direction);
        }
    }

    // Plays the particle effect on the owner client immediately (no server round-trip)
    [ClientRpc]
    private void PlayVFXClientRpc()
    {
        if (IsOwner && repulsorBlast != null)
            repulsorBlast.Play();
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 pos, Vector3 direction)
    {
        GetComponentInParent<OxygenManager>()?.DrainOxygen(oxygenCostPerShot);

        var proj = Instantiate(projectilePrefab, pos, Quaternion.LookRotation(direction));
        proj.Spawn();

        var rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = direction * projectileSpeed;
    }
}