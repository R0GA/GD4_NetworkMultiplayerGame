using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Tracks task completion for the slug player.
/// 
/// Ownership model:
///   • One TaskManager NetworkObject lives in the scene.
///   • It is owned by the slug client (the one who has the SlugPlayer).
///   • On spawn, it calls GameManager.RegisterTaskManager() so the GameManager
///     can listen for OnAllTasksCompleted.
///   • BaseTask components live on (or are children of) the slug player prefab.
///     The manager discovers them via GetComponentsInChildren after the slug
///     player registers itself (see RegisterSlugPlayer below).
/// </summary>
public class TaskManager : NetworkBehaviour
{
    [Header("Task Registry")]
    [Tooltip("Leave empty — tasks are discovered from the slug player prefab at runtime.")]
    [SerializeField] private List<BaseTask> allTasks = new();

    [Header("UI  (owner client only)")]
    [SerializeField] private GameObject taskListPanel;
    [Tooltip("One GameObject per task, same order as allTasks. Hidden when that task is done.")]
    [SerializeField] private List<GameObject> taskListItems = new();

    public UnityEvent OnAllTasksCompleted = new();

    // Bitmask — bit i is set when task i is complete.
    private readonly NetworkVariable<int> completedTasksMask = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private bool allDone = false;

    // -------------------------------------------------------------------------
    // Network lifecycle
    // -------------------------------------------------------------------------

    public override void OnNetworkSpawn()
    {
        completedTasksMask.OnValueChanged += OnCompletedMaskChanged;

        // Show task HUD only on the owning (slug) client.
        if (taskListPanel != null)
            taskListPanel.SetActive(IsOwner);

        // Tell the GameManager this manager exists so it can subscribe to
        // OnAllTasksCompleted regardless of spawn order.
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterTaskManager(this);
    }

    public override void OnNetworkDespawn()
    {
        completedTasksMask.OnValueChanged -= OnCompletedMaskChanged;
    }

    // -------------------------------------------------------------------------
    // Called by SlugPlayer (or a bootstrapper) once the player prefab exists
    // -------------------------------------------------------------------------

    /// <summary>
    /// Discovers all BaseTask components on <paramref name="slugPlayerRoot"/>
    /// and initialises them. Call this from SlugPlayer.OnNetworkSpawn on the
    /// owning client (or from any point after the prefab is instantiated).
    /// </summary>
    public void RegisterSlugPlayer(GameObject slugPlayerRoot)
    {
        if (allTasks.Count == 0)
        {
            allTasks.AddRange(slugPlayerRoot.GetComponentsInChildren<BaseTask>(true));

            // Also pick up any scene-level tasks (terminals, panels, etc.)
            // that are not children of the player prefab.
            allTasks.AddRange(FindObjectsByType<BaseTask>(FindObjectsSortMode.None));

            // De-duplicate in case both sources returned the same component.
            var seen = new HashSet<BaseTask>();
            var dedup = new List<BaseTask>();
            foreach (var t in allTasks)
                if (t != null && seen.Add(t)) dedup.Add(t);
            allTasks = dedup;
        }

        for (int i = 0; i < allTasks.Count; i++)
            allTasks[i].Initialise(this, i);

        Debug.Log($"[TaskManager] Initialised {allTasks.Count} task(s).");
    }

    // -------------------------------------------------------------------------
    // Called by BaseTask on the owning client when a task finishes
    // -------------------------------------------------------------------------

    public void NotifyTaskCompleted(int taskIndex)
    {
        if (!IsOwner) return;
        MarkTaskCompleteServerRpc(taskIndex);
    }

    // -------------------------------------------------------------------------
    // Server RPC — sets the shared bitmask
    // -------------------------------------------------------------------------

    [ServerRpc]
    private void MarkTaskCompleteServerRpc(int taskIndex)
    {
        if (taskIndex < 0 || taskIndex >= allTasks.Count)
        {
            Debug.LogWarning($"[TaskManager] Invalid task index {taskIndex}");
            return;
        }

        completedTasksMask.Value |= (1 << taskIndex);
        CheckAllComplete();
    }

    // -------------------------------------------------------------------------
    // Reacts to mask change on ALL clients
    // -------------------------------------------------------------------------

    private void OnCompletedMaskChanged(int previous, int current)
    {
        int newlyCompleted = current & ~previous;

        // Update HUD on the owner client.
        if (IsOwner)
        {
            for (int i = 0; i < taskListItems.Count; i++)
            {
                if (taskListItems[i] == null) continue;
                bool done = (current & (1 << i)) != 0;
                taskListItems[i].SetActive(!done);
            }
        }

        // Trigger alarms everywhere for every newly-completed task.
        if (newlyCompleted != 0)
            TriggerAlarms(newlyCompleted);

        CheckAllComplete();
    }

    // -------------------------------------------------------------------------
    // Alarms — run on every client
    // -------------------------------------------------------------------------

    private void TriggerAlarms(int newlyCompletedMask)
    {
        var allTriggers = FindObjectsByType<TaskTrigger>(FindObjectsSortMode.None);

        for (int i = 0; i < allTasks.Count; i++)
        {
            if ((newlyCompletedMask & (1 << i)) == 0) continue;

            string id = allTasks[i].taskIdentifier;
            foreach (var trigger in allTriggers)
                if (trigger.TaskIdentifier == id)
                    trigger.ActivateAlarmLights();
        }

        if (ShipAudioManager.Instance != null)
            ShipAudioManager.Instance.PlayAlarm();
        else
            Debug.LogWarning("[TaskManager] ShipAudioManager.Instance is null.");
    }

    // -------------------------------------------------------------------------
    // Completion check
    // -------------------------------------------------------------------------

    private void CheckAllComplete()
    {
        if (allDone || allTasks.Count == 0) return;

        int fullMask = (1 << allTasks.Count) - 1;
        if ((completedTasksMask.Value & fullMask) != fullMask) return;

        allDone = true;
        Debug.Log("[TaskManager] All tasks completed!");
        OnAllTasksCompleted?.Invoke();
    }

    // -------------------------------------------------------------------------
    // Utilities
    // -------------------------------------------------------------------------

    public bool IsTaskComplete(int index) =>
        index >= 0 && index < allTasks.Count && (completedTasksMask.Value & (1 << index)) != 0;

    public int CompletedCount()
    {
        int count = 0, mask = completedTasksMask.Value;
        while (mask != 0) { count += mask & 1; mask >>= 1; }
        return count;
    }

    public int TotalCount() => allTasks.Count;
}