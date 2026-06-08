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

    [Header("Shooting Cost")]
    [SerializeField] private float oxygenCostPerShot = 2f;

    private OxygenManager oxygenManager;

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
        {
            playerCamera = player.PlayerCamera;
            oxygenManager = player.GetComponent<OxygenManager>(); // ADD THIS
        }

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
            Vector3 direction = GetAimDirection();
            ShootServerRPC(firePoint.position, direction);
        }
    }

    private Vector3 GetAimDirection()
    {
        if (playerCamera == null)
            return firePoint.forward;

        Vector3 targetPoint = playerCamera.transform.position + playerCamera.transform.forward * targetDistance;

        Vector3 direction = (targetPoint - firePoint.position).normalized;

        return direction;
    }

    [ServerRpc]
    private void ShootServerRPC(Vector3 pos, Vector3 direction)
    {
        GetComponentInParent<OxygenManager>()?.DrainOxygen(oxygenCostPerShot);

        var proj = Instantiate(projectilePrefab, pos, Quaternion.LookRotation(direction));
        proj.Spawn();
        var rb = proj.GetComponent<Rigidbody>();
        rb.linearVelocity = direction * projectileSpeed;
        repulsorBlast.Play();
    }
}