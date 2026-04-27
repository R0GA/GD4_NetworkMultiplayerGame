using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class NetworkFPSPlayer : NetworkBehaviour
{
    [Header("Player Components")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Camera playerCamera;

    [Header("Player Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float lookSensitivity = 2f;
    [SerializeField] private float maxPitch = 80f;
    [SerializeField] private float jumpHeight = 2f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";

    private PlayerInput pi;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private CharacterController cc; 
    private bool isGrounded;
    private Vector3 velocity;

    private float pitch;

    public override void OnNetworkSpawn()
    {
        cc = GetComponent<CharacterController>();
        pi = GetComponent<PlayerInput>();

        if (!IsOwner)
        {
            if (playerCamera) playerCamera.enabled = false;
            if (pi) pi.enabled = false;
            return;
        }

        moveAction = pi.actions["Move"];
        lookAction = pi.actions["Look"];
        jumpAction = pi.actions["Jump"];
        moveAction.Enable();
        lookAction.Enable();
        jumpAction.Enable();

        if (playerCamera) playerCamera.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (IsOwner)
        {
            Vector2 m = moveAction.ReadValue<Vector2>();
            Vector3 move = transform.right * m.x + transform.forward * m.y;
            cc.Move(move * moveSpeed * Time.deltaTime);

            Vector2 look = lookAction.ReadValue<Vector2>() * lookSensitivity;
            transform.Rotate(0f, look.x, 0f);

            pitch -= look.y;
            pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);
            cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);

            if (jumpAction.WasPressedThisFrame())
                Jump(jumpHeight);

            if (animator) animator.SetFloat(speedParam, m.magnitude);

            GroundCheck();
            ApplyGravity();
        }
    }

    public void ApplyGravity() 
    {
        if (isGrounded && velocity.y < 0)
            velocity.y = -0.5f;   
        else
            velocity.y += Physics.gravity.y * Time.deltaTime;

        cc.Move(velocity * Time.deltaTime);
    }
    public void Jump(float jumpHeight) 
    {
        Debug.Log("Jumping with height: " + jumpHeight);
        if (isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
    }
    public void GroundCheck()
    {
        Ray groundCheck = new Ray(transform.position + Vector3.up * 0.1f, Vector3.down);
        RaycastHit hit;
        bool grounded = Physics.Raycast(groundCheck, out hit, 1.2f);
        isGrounded = grounded;
        //Debug.Log("Grounded: " + isGrounded);
    }
}
