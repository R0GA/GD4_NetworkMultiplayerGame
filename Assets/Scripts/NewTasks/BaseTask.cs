using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Abstract base for every sabotage mini-game task.
/// 
/// Workflow:
///   1. The task GameObject sits dormant in the scene.
///   2. A TaskTrigger (collider / interactable) calls TryOpen() when the
///      saboteur player interacts with it.
///   3. The task opens its UI panel, locks player look/move via SlugPlayer.SetUIMode,
///      and runs its mini-game logic.
///   4. On success the task calls CompleteTask(); on failure / cancel it calls CloseTask().
/// </summary>
public abstract class BaseTask : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("Base Task")]
    [SerializeField] protected GameObject taskPanel;
    [SerializeField] public string taskDisplayName = "Task";

    [Tooltip("Unique ID that matches the TaskTrigger's taskIdentifier.")]
    [SerializeField] public string taskIdentifier = "";

    // ── Events ───────────────────────────────────────────────────────────────

    /// <summary>Fired locally when the player completes the task.</summary>
    public UnityEvent OnTaskCompleted = new();

    // ── Runtime ──────────────────────────────────────────────────────────────

    protected TaskManager taskManager;
    protected SlugPlayer currentPlayer;
    protected int taskIndex = -1;
    private bool isComplete = false;
    private bool isOpen = false;

    // ═════════════════════════════════════════════════════════════════════════
    // Initialisation (called by TaskManager)
    // ═════════════════════════════════════════════════════════════════════════

    public void Initialise(TaskManager manager, int index)
    {
        taskManager = manager;
        taskIndex = index;

        if (taskPanel) taskPanel.SetActive(false);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Public API
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by the world interactable when the local saboteur player
    /// walks up and presses interact. Safe to call even if already complete.
    /// </summary>
    public bool TryOpen(SlugPlayer player)
    {
        if (isComplete || isOpen) return false;

        currentPlayer = player;
        isOpen = true;

        if (taskPanel) taskPanel.SetActive(true);

        // Lock player movement / camera
        if (currentPlayer) currentPlayer.SetUIMode(true);

        OnOpen();
        return true;
    }

    /// <summary>
    /// Close the task UI without completing it (player pressed cancel, etc.).
    /// </summary>
    public void CloseTask()
    {
        if (!isOpen) return;
        isOpen = false;

        if (taskPanel) taskPanel.SetActive(false);

        if (currentPlayer) currentPlayer.SetUIMode(false);

        OnClose();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Protected Helpers for subclasses
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Call this from the subclass when the mini-game is won.</summary>
    protected void CompleteTask()
    {
        if (isComplete) return;
        isComplete = true;
        isOpen = false;

        if (taskPanel) taskPanel.SetActive(false);
        if (currentPlayer) currentPlayer.SetUIMode(false);

        OnTaskCompleted?.Invoke();
        taskManager?.NotifyTaskCompleted(taskIndex);

        Debug.Log($"[BaseTask] '{taskDisplayName}' completed (index {taskIndex}).");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Abstract / Virtual hooks for subclasses
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Called just after the panel is shown. Start your mini-game here.</summary>
    protected abstract void OnOpen();

    /// <summary>Called when the task is closed without completing. Reset state.</summary>
    protected virtual void OnClose() { }

    // ═════════════════════════════════════════════════════════════════════════
    // Getters
    // ═════════════════════════════════════════════════════════════════════════

    public bool IsComplete => isComplete;
    public bool IsOpen => isOpen;
}
