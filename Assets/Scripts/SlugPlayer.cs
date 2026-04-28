using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class SlugPlayer : NetworkBehaviour
{
    [Header("Components")]
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private Transform playerVisualRoot;
    [SerializeField] private Canvas taskCanvas;
    private AudioListener audioListener;

    [SerializeField] private CinemachineInputAxisController cinemachineInputController;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float propMoveSpeedMultiplier = 0.55f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Look / Rotation")]
    [SerializeField] private float horizontalLookSensitivity = 2f;
    [SerializeField] private bool ignoreVerticalLook = true;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";

    [Header("Prop Transform")]
    [SerializeField] private float interactRange = 4f;
    [SerializeField] private LayerMask propLayerMask = ~0;
    [SerializeField] private GameObject interactHintUI;
    [SerializeField] private GameObject pickupHintUI;

    [Header("Pick Up Settins")]
    [SerializeField] private Transform holdPoint;
    [SerializeField] private float pickUpRange;
    [SerializeField] private GameObject heldObject;
    [SerializeField] private bool isHolding;
    [SerializeField] private LayerMask pickUpLayer = ~0;

    private bool isInUIMode = false;

    private PlayerInput pi;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction transformAction;
    private InputAction pickUpAction;

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
        audioListener = mainCamera ? mainCamera.GetComponent<AudioListener>() : null;

        networkPropIndex.OnValueChanged += OnPropIndexChanged;

        if (networkPropIndex.Value >= 0)
            ApplyPropVisual(networkPropIndex.Value);

        if (!IsOwner)
        {
            if (mainCamera) mainCamera.enabled = false;
            if (audioListener) audioListener.enabled = false;
            if (pi) pi.enabled = false;
            if (interactHintUI) interactHintUI.SetActive(false);
            if (pickupHintUI) pickupHintUI.SetActive(false);
            if (taskCanvas) taskCanvas.enabled = false;
            return;
        }

        if (mainCamera) mainCamera.enabled = true;
        if (audioListener) audioListener.enabled = true;
        if (taskCanvas) taskCanvas.enabled = true;
        SetupInput();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log($"[SlugPlayer] After SetupInput - moveAction.enabled={moveAction.enabled}, lookAction.enabled={lookAction.enabled}, jumpAction.enabled={jumpAction.enabled}");
    }

    public override void OnNetworkDespawn()
    {
        networkPropIndex.OnValueChanged -= OnPropIndexChanged;

        if (IsOwner) SetUIMode(false);
    }

    private void SetupInput()
    {
        if (!IsOwner) return;

        moveAction = pi.actions["Move"];
        lookAction = pi.actions["Look"];
        jumpAction = pi.actions["Jump"];
        transformAction = pi.actions["Transform"];
        pickUpAction = pi.actions["PickUp"];

        moveAction.Enable();
        lookAction.Enable();
        jumpAction.Enable();
        transformAction.Enable();
        pickUpAction.Enable();
    }

    private void Update()
    {
        if (!IsOwner) return;

       
        if (isInUIMode) return;
       
        GroundCheck();
        HandleMovement();
        HandleTransformInput();
        ApplyGravity();
        UpdateInteractHint();
        UpdatePickupHint();

        if (animator)
            animator.SetFloat(speedParam, moveAction.ReadValue<Vector2>().magnitude);

        Debug.DrawRay(gameObject.transform.position, gameObject.transform.forward * pickUpRange, Color.red, 2f);

        if (pickUpAction.WasPressedThisFrame()) PickUp();

        if (Time.frameCount % 60 == 0) 
        {
            //Debug.Log($"[SlugPlayer] moveAction.ReadValue<Vector2>() = {moveAction.ReadValue<Vector2>()}");
        }
    }


    public void SetUIMode(bool uiActive)
    {
        if (!IsOwner) return;

        isInUIMode = uiActive;

        if (cinemachineInputController != null)
            cinemachineInputController.enabled = !uiActive;

        if (lookAction != null)
        {
            if (uiActive) lookAction.Disable();
            else lookAction.Enable();
        }

        if (uiActive) velocity = Vector3.zero;

        Cursor.lockState = uiActive ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = uiActive;
    }
    public bool GetUIMode() => isInUIMode;

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
            RequestTransformServerRpc(-1);
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
    private void UpdatePickupHint()
    {
        if (!pickupHintUI) return;
        bool show = !isHolding && GetPickupTarget() != null;
        if (pickupHintUI.activeSelf != show)
            pickupHintUI.SetActive(show);
    }

    private GameObject GetPickupTarget()
    {
        if (!mainCamera) return null;
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, pickUpRange, pickUpLayer))
            return hit.collider.gameObject;
        return null;
    }

    [ServerRpc]
    private void RequestTransformServerRpc(int propId)
    {
        if (propId != -1 && !PropInteractable.Registry.ContainsKey(propId))
        {
            Debug.LogWarning($"[SlugPlayer] Server rejected unknown prop id {propId}.");
            return;
        }
        networkPropIndex.Value = propId;
    }

    private void OnPropIndexChanged(int previous, int current)
    {
        ApplyPropVisual(current);
    }

    private void ApplyPropVisual(int propId)
    {
        if (spawnedPropVisual != null)
        {
            Destroy(spawnedPropVisual);
            spawnedPropVisual = null;
        }

        if (propId < 0)
        {
            if (playerVisualRoot) playerVisualRoot.gameObject.SetActive(true);
            ResetCharacterController();
            return;
        }

        if (!PropInteractable.Registry.TryGetValue(propId, out PropInteractable prop))
        {
            Debug.LogWarning($"[SlugPlayer] Prop id {propId} not found in registry.");
            return;
        }

        if (playerVisualRoot) playerVisualRoot.gameObject.SetActive(false);

        spawnedPropVisual = Instantiate(prop.gameObject, transform);
        spawnedPropVisual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        spawnedPropVisual.name = $"[PropVisual] {prop.DisplayName}";

        foreach (var rb in spawnedPropVisual.GetComponentsInChildren<Rigidbody>()) Destroy(rb);
        foreach (var col in spawnedPropVisual.GetComponentsInChildren<Collider>()) Destroy(col);
        foreach (var pai in spawnedPropVisual.GetComponentsInChildren<PropInteractable>()) Destroy(pai);

        FitCharacterControllerToProp(spawnedPropVisual);
    }

    private void FitCharacterControllerToProp(GameObject propVisual)
    {
        Renderer[] renderers = propVisual.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds combined = renderers[0].bounds;
        foreach (var r in renderers) combined.Encapsulate(r.bounds);

        float height = Mathf.Max(combined.size.y, 0.3f);
        float radius = Mathf.Max(combined.size.x, combined.size.z) * 0.5f;
        radius = Mathf.Clamp(radius, 0.05f, height * 0.5f);

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

    public void PickUp()
    {
        if(heldObject != null)
        {
            heldObject.GetComponent<Rigidbody>().isKinematic = false;
            heldObject.transform.parent = null;
            isHolding = false;
            
        }

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        Debug.DrawRay(gameObject.transform.position, gameObject.transform.forward*pickUpRange, Color.red, 2f);


        if(Physics.Raycast(ray,out hit,pickUpRange, pickUpLayer))
        { 
            Debug.Log("Hit");
            heldObject = hit.collider.gameObject;
            heldObject.GetComponent<Rigidbody>().isKinematic = true;

            heldObject.transform.position = holdPoint.position;
            heldObject.transform.rotation = holdPoint.rotation;
            heldObject.transform.parent = holdPoint;
            isHolding=true;
           
        }
    }

    
    
}