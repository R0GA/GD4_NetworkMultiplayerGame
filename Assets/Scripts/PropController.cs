using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PropController : NetworkBehaviour
{
    [Header ("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 100f;

    [Header("Player Components")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private PlayerInput input;

    public override void OnNetworkSpawn()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInput>();

        if (!IsOwner)
        {
            enabled = false;
            return;
        }
    }
}
