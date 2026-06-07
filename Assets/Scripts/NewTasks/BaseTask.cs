using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public abstract class BaseTask : MonoBehaviour
{
    [Header("Base Task")]
    [SerializeField] protected GameObject taskPanel;
    [SerializeField] public string taskDisplayName = "Task";

    [Tooltip("Unique ID that matches the TaskTrigger's taskIdentifier.")]
    [SerializeField] public string taskIdentifier = "";

    [Header("Input")]
    [Tooltip("Must match the interactActionName on the TaskTrigger that opens this task.")]
    [SerializeField] private string interactActionName = "Interact";

    public UnityEvent OnTaskCompleted = new();

    protected TaskManager taskManager;
    protected SlugPlayer currentPlayer;
    protected int taskIndex = -1;
    private bool isComplete = false;
    private bool isOpen = false;

    // Cached reference to the interact action so we can unsubscribe cleanly.
    private InputAction interactAction;

    public void Initialise(TaskManager manager, int index)
    {
        taskManager = manager;
        taskIndex = index;

        if (taskPanel) taskPanel.SetActive(false);
    }

    public bool TryOpen(SlugPlayer player)
    {
        if (isComplete || isOpen) return false;

        currentPlayer = player;
        isOpen = true;

        if (taskPanel) taskPanel.SetActive(true);

        if (currentPlayer) currentPlayer.SetUIMode(true);

        // Re-use the same interact action to close the task on a second press.
        SubscribeInteractAction(player);

        OnOpen();
        return true;
    }

    public void CloseTask()
    {
        if (!isOpen) return;
        isOpen = false;

        if (taskPanel) taskPanel.SetActive(false);

        if (currentPlayer) currentPlayer.SetUIMode(false);

        UnsubscribeInteractAction();

        OnClose();
    }

    protected void CompleteTask()
    {
        if (isComplete) return;
        isComplete = true;
        isOpen = false;

        if (taskPanel) taskPanel.SetActive(false);
        if (currentPlayer) currentPlayer.SetUIMode(false);

        // No longer need the interact binding once the task is finished.
        UnsubscribeInteractAction();

        OnTaskCompleted?.Invoke();
        taskManager?.NotifyTaskCompleted(taskIndex);

        Debug.Log($"[BaseTask] '{taskDisplayName}' completed (index {taskIndex}).");
    }

    // -------------------------------------------------------------------------
    // Interact-to-close handling
    // -------------------------------------------------------------------------

    private void SubscribeInteractAction(SlugPlayer player)
    {
        var pi = player.GetComponent<PlayerInput>();
        if (pi == null) return;

        var action = pi.actions.FindAction(interactActionName, throwIfNotFound: false);
        if (action == null) return;

        interactAction = action;
        interactAction.performed += OnInteractPerformed;
    }

    private void UnsubscribeInteractAction()
    {
        if (interactAction == null) return;
        interactAction.performed -= OnInteractPerformed;
        interactAction = null;
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        // TaskTrigger already guards re-opening via GetUIMode(), so this fires
        // only when the task is open and the player presses interact again.
        if (isOpen && !isComplete)
            CloseTask();
    }

    // -------------------------------------------------------------------------
    // Overridable hooks
    // -------------------------------------------------------------------------

    protected abstract void OnOpen();
    protected virtual void OnClose() { }

    public bool IsComplete => isComplete;
    public bool IsOpen => isOpen;
}