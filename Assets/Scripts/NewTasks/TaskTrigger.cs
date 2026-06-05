using UnityEngine;
using UnityEngine.Events;

public class TaskTrigger : MonoBehaviour
{
    [Header("Task")]
    [Tooltip("Must match the taskIdentifier of the desired BaseTask on the player.")]
    [SerializeField] private string taskIdentifier = "";

    [Header("Prompt UI")]
    [SerializeField] private GameObject interactPrompt;

    [Header("Input")]
    [SerializeField] private string interactActionName = "Interact";

    public UnityEvent OnInteracted = new();

    private SlugPlayer playerInRange;
    private UnityEngine.InputSystem.InputAction interactAction;

    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponentInParent<SlugPlayer>();
        if (player == null || !player.IsOwner) return;

        // If the task is already complete, do nothing
        BaseTask task = player.GetTaskByIdentifier(taskIdentifier);
        if (task == null || task.IsComplete) return;

        playerInRange = player;

        // Setup input
        var pi = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (pi != null)
        {
            var action = pi.actions.FindAction(interactActionName, throwIfNotFound: false);
            if (action != null)
            {
                interactAction = action;
                interactAction.performed += OnInteractPerformed;
            }
        }

        if (interactPrompt) interactPrompt.SetActive(true);
    }
    private void OnTriggerExit(Collider other)
    {
        var player = other.GetComponentInParent<SlugPlayer>();
        if (player == null || !player.IsOwner) return;
        if (player != playerInRange) return;
        CleanUp();
    }

    private void OnInteractPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (playerInRange == null) return;
        if (playerInRange.GetUIMode()) return; // already in another UI

        BaseTask task = playerInRange.GetTaskByIdentifier(taskIdentifier);
        if (task == null || task.IsComplete)
        {
            CleanUp();
            return;
        }

        TryOpenTask();
    }

    public void TryOpenTask()
    {
        if (playerInRange == null) return;
        BaseTask task = playerInRange.GetTaskByIdentifier(taskIdentifier);
        if (task == null) return;

        if (task.TryOpen(playerInRange))
        {
            OnInteracted?.Invoke();
            if (interactPrompt) interactPrompt.SetActive(false);
        }
    }

    private void CleanUp()
    {
        if (interactAction != null)
        {
            interactAction.performed -= OnInteractPerformed;
            interactAction = null;
        }
        playerInRange = null;

        if (interactPrompt && (playerInRange == null || !playerInRange.GetTaskByIdentifier(taskIdentifier)?.IsOpen == true))
            interactPrompt.SetActive(false);
    }

    private void OnDestroy() => CleanUp();
}