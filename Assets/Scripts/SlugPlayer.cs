using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;  


[RequireComponent(typeof(CharacterController))]
public class SlugPlayer : NetworkBehaviour
{
   
    [Header("Components")]
    [Tooltip("Cinemachine Virtual Camera that follows this player.")]
    [SerializeField] private CinemachineCamera virtualCamera;
    [Tooltip("Root Transform of the player's own 3D model — hidden while disguised.")]
    [SerializeField] private Transform playerVisualRoot;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpHeight = 2f;
    [Tooltip("Speed multiplier applied while disguised as a prop.")]
    [SerializeField] private float propMoveSpeedMultiplier = 0.55f;
    [Tooltip("How fast the player rotates to face movement direction.")]
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Look / Rotation")]
    [SerializeField] private float horizontalLookSensitivity = 2f;
    [Tooltip("If true, vertical mouse input is ignored (let Cinemachine handle pitch).")]
    [SerializeField] private bool ignoreVerticalLook = true;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";

    [Header("Prop Transform")]
    [Tooltip("Max distance (metres) at which a prop can be targeted for disguise.")]
    [SerializeField] private float interactRange = 4f;
    [Tooltip("Layer mask for the prop‑targeting raycast. Include all layers your props live on.")]
    [SerializeField] private LayerMask propLayerMask = ~0;
    [Tooltip("On‑screen hint shown when a prop is in range. Hook up a UI Text/TMP element.")]
    [SerializeField] private GameObject interactHintUI;

    private PlayerInput pi;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction transformAction;

    private CharacterController cc;
    private Vector3 velocity;
    private bool isGrounded;

    private float ccOriginalHeight;
    private float ccOriginalRadius;
    private Vector3 ccOriginalCenter;

    private readonly NetworkVariable<int> networkPropIndex = new(
        value: -1,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );

    private GameObject spawnedPropVisual;

    public bool IsTransformed => networkPropIndex.Value >= 0;

    private Camera mainCamera;


    public override void OnNetworkSpawn()
    {
        cc = GetComponent<CharacterController>();
        pi = GetComponent<PlayerInput>();

        ccOriginalHeight = cc.height;
        ccOriginalRadius = cc.radius;
        ccOriginalCenter = cc.center;

        mainCamera = GetComponentInChildren<Camera>();

        networkPropIndex.OnValueChanged += OnPropIndexChanged;

        if (networkPropIndex.Value >= 0)
            ApplyPropVisual(networkPropIndex.Value);

        if (!IsOwner)
        {
            if (mainCamera) mainCamera.enabled = false;
            if (pi) pi.enabled = false;
            if (interactHintUI) interactHintUI.SetActive(false);
            return;
        }

        SetupInput();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public override void OnNetworkDespawn()
    {
        networkPropIndex.OnValueChanged -= OnPropIndexChanged;
    }

    private void SetupInput()
    {
        moveAction = pi.actions["Move"];
        lookAction = pi.actions["Look"];
        jumpAction = pi.actions["Jump"];
        transformAction = pi.actions["Transform"];

        moveAction.Enable();
        lookAction.Enable();
        jumpAction.Enable();
        transformAction.Enable();
    }

    private void Update()
    {
        if (!IsOwner) return;

        GroundCheck();
        HandleMovement();
        HandleTransformInput();
        ApplyGravity();
        UpdateInteractHint();

        if (animator)
            animator.SetFloat(speedParam, moveAction.ReadValue<Vector2>().magnitude);
    }

    private void HandleMovement()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();


        Vector3 forward = mainCamera.transform.forward;
        Vector3 right = mainCamera.transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 desiredMoveDirection = right * input.x + forward * input.y;
        float speed = IsTransformed ? moveSpeed * propMoveSpeedMultiplier : moveSpeed;

        if (desiredMoveDirection.magnitude > 0.01f)
        {
            cc.Move(desiredMoveDirection * (speed * Time.deltaTime));

            Quaternion targetRotation = Quaternion.LookRotation(desiredMoveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (!IsTransformed && jumpAction.WasPressedThisFrame())
            Jump();
    }
    private void ApplyGravity()
    {
        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f;
        else
            velocity.y += Physics.gravity.y * Time.deltaTime;

        cc.Move(velocity * Time.deltaTime);
    }

    private void Jump()
    {
        if (isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
    }

    private void GroundCheck()
    {
        float castDistance = cc.height * 0.5f - cc.radius + cc.skinWidth + 0.05f;
        Vector3 origin = transform.position + Vector3.up * (cc.height * 0.5f);
        isGrounded = Physics.SphereCast(origin, cc.radius * 0.9f, Vector3.down, out _, castDistance);
    }

    private void HandleTransformInput()
    {
        if (!transformAction.WasPressedThisFrame()) return;

        if (IsTransformed)
        {
            RequestTransformServerRpc(-1);
        }
        else
        {
            PropInteractable target = GetTargetProp();
            if (target != null)
                RequestTransformServerRpc(target.PropIndex);
        }
    }

    private void UpdateInteractHint()
    {
        if (!interactHintUI) return;
        bool show = !IsTransformed && GetTargetProp() != null;
        if (interactHintUI.activeSelf != show)
            interactHintUI.SetActive(show);
    }

    // ──────────────────────────────────────────────
    // Networking — ServerRpc & NetworkVariable callback
    // ──────────────────────────────────────────────

    /// <summary>
    /// Sent from the owning client to the server to request a disguise change.
    /// Pass -1 to revert to the normal player.
    /// </summary>
    [ServerRpc]
    private void RequestTransformServerRpc(int propIndex)
    {
        if (propIndex != -1 &&
            (propIndex < 0 || propIndex >= PropInteractable.Registry.Count))
        {
            Debug.LogWarning($"[SlugPlayer] Server rejected invalid prop index {propIndex}.");
            return;
        }

        networkPropIndex.Value = propIndex;
    }

    /// <summary>
    /// Called on ALL clients (including server/host) whenever networkPropIndex changes.
    /// </summary>
    private void OnPropIndexChanged(int previous, int current)
    {
        ApplyPropVisual(current);
    }

    private void ApplyPropVisual(int propIndex)
    {
        // Tear down any existing prop visual first
        if (spawnedPropVisual != null)
        {
            Destroy(spawnedPropVisual);
            spawnedPropVisual = null;
        }

        if (propIndex < 0)
        {
            // ── Revert to player ──────────────────────────────
            if (playerVisualRoot) playerVisualRoot.gameObject.SetActive(true);
            ResetCharacterController();
            return;
        }

        if (propIndex >= PropInteractable.Registry.Count)
        {
            Debug.LogWarning($"[SlugPlayer] Prop index {propIndex} not found in registry.");
            return;
        }

        PropInteractable prop = PropInteractable.Registry[propIndex];

        // ── Swap visuals ──────────────────────────────────────
        if (playerVisualRoot) playerVisualRoot.gameObject.SetActive(false);

        // Duplicate the prop as a parented child — all clients do this independently
        spawnedPropVisual = Instantiate(prop.gameObject, transform);
        spawnedPropVisual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        spawnedPropVisual.name = $"[PropVisual] {prop.DisplayName}";

        // Strip physics and gameplay logic from the visual copy so it doesn't interfere
        foreach (var rb in spawnedPropVisual.GetComponentsInChildren<Rigidbody>()) Destroy(rb);
        foreach (var col in spawnedPropVisual.GetComponentsInChildren<Collider>()) Destroy(col);
        foreach (var pai in spawnedPropVisual.GetComponentsInChildren<PropInteractable>()) Destroy(pai);

        // Resize the CharacterController to wrap the prop's visual bounds
        FitCharacterControllerToProp(spawnedPropVisual);
    }

    /// <summary>
    /// Resizes the CharacterController so its capsule approximately wraps the prop's renderers.
    /// Called after the prop visual has been parented and positioned.
    /// </summary>
    private void FitCharacterControllerToProp(GameObject propVisual)
    {
        Renderer[] renderers = propVisual.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        // Accumulate world‑space bounds across all child renderers
        Bounds combined = renderers[0].bounds;
        foreach (var r in renderers) combined.Encapsulate(r.bounds);

        // Derive capsule dimensions from the bounding box
        float height = Mathf.Max(combined.size.y, 0.3f);
        float radius = Mathf.Max(combined.size.x, combined.size.z) * 0.5f;
        // CharacterController constraint: radius must not exceed half the height
        radius = Mathf.Clamp(radius, 0.05f, height * 0.5f);

        // Calculate the capsule centre's Y offset relative to the player root
        float localCentreY = combined.center.y - transform.position.y;

        cc.height = height;
        cc.radius = radius;
        cc.center = new Vector3(0f, localCentreY, 0f);
    }

    private void ResetCharacterController()
    {
        cc.height = ccOriginalHeight;
        cc.radius = ccOriginalRadius;
        cc.center = ccOriginalCenter;
    }

    private PropInteractable GetTargetProp()
    {
        if (!mainCamera) return null;

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, propLayerMask))
            return hit.collider.GetComponentInParent<PropInteractable>();

        return null;
    }
}