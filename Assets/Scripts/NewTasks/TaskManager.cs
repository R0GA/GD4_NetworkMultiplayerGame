using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;


public class TaskManager : NetworkBehaviour
{
    [Header("Task Registry")]
    [Tooltip("Leave empty — tasks are discovered from the slug player prefab at runtime.")]
    [SerializeField] private List<BaseTask> allTasks = new();

    [Header("UI  (slug's machine only)")]
    [SerializeField] private GameObject taskListPanel;
    [Tooltip("One GameObject per task, same order as allTasks. Hidden when that task is done.")]
    [SerializeField] private List<GameObject> taskListItems = new();

    public UnityEvent OnAllTasksCompleted = new();

    private readonly NetworkVariable<int> completedTasksMask = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private bool allDone = false;
    private bool tasksBuilt = false;

    // True on the machine that is controlling the slug (shows the task HUD).
    // NOTE: this replaces the old IsOwner checks. TaskManager is a scene object,
    // so it is owned by the SERVER, which meant the HUD and task completion only
    // worked when the host was the slug.
    private bool isLocalSlugViewer = false;


    public override void OnNetworkSpawn()
    {
        completedTasksMask.OnValueChanged += OnCompletedMaskChanged;

        // Hidden until RegisterSlugPlayer marks this machine as the slug's.
        // Uses the current flag (not a hard 'false') so a slug that registered
        // before this ran doesn't get its HUD switched off again.
        if (taskListPanel != null)
            taskListPanel.SetActive(isLocalSlugViewer);

        // Register on server only — EndGame calls NetworkManager.SceneManager.LoadScene
        // which must run server-side, so the listener must be attached there too.
        if (IsServer)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.RegisterTaskManager(this);
            else
                Debug.LogWarning("[TaskManager] GameManager.Instance is null on spawn.");
        }
    }

    public override void OnNetworkDespawn()
    {
        completedTasksMask.OnValueChanged -= OnCompletedMaskChanged;
    }

    /// <summary>
    /// Discovers all BaseTask components and initialises them.
    /// Call from SlugPlayer.OnNetworkSpawn on EVERY peer (the server needs the
    /// task list to validate completion RPCs), passing isLocalOwner = IsOwner.
    /// </summary>
    public void RegisterSlugPlayer(GameObject slugPlayerRoot, bool isLocalOwner = true)
    {
        if (!tasksBuilt)
        {
            if (allTasks.Count == 0)
            {
                allTasks.AddRange(slugPlayerRoot.GetComponentsInChildren<BaseTask>(true));

                // Also pick up any scene-level tasks (terminals, panels, etc.),
                // including inactive ones so every peer sees the same set.
                allTasks.AddRange(FindObjectsByType<BaseTask>(FindObjectsInactive.Include, FindObjectsSortMode.None));

                // De-duplicate in case both sources returned the same component.
                var seen = new HashSet<BaseTask>();
                var dedup = new List<BaseTask>();
                foreach (var t in allTasks)
                    if (t != null && seen.Add(t)) dedup.Add(t);

                // Task indices are sent over the network, so every peer MUST end up
                // with the same order. FindObjectsByType gives no ordering guarantee,
                // so sort deterministically (taskIdentifier must be unique per task).
                dedup.Sort((a, b) =>
                {
                    int c = string.CompareOrdinal(a.taskIdentifier, b.taskIdentifier);
                    return c != 0 ? c : string.CompareOrdinal(a.name, b.name);
                });

                allTasks = dedup;
            }
            tasksBuilt = true;
        }

        for (int i = 0; i < allTasks.Count; i++)
            allTasks[i].Initialise(this, i);

        if (isLocalOwner)
        {
            isLocalSlugViewer = true;
            if (taskListPanel != null)
            {
                taskListPanel.SetActive(true);
                Debug.Log($"[TaskManager] Task list shown (panel active={taskListPanel.activeInHierarchy}, items={taskListItems.Count}).");
            }
            else
                Debug.LogWarning("[TaskManager] taskListPanel is not assigned, so the task list cannot be shown.");
            RefreshTaskListHud(completedTasksMask.Value);
        }

        Debug.Log($"[TaskManager] Initialised {allTasks.Count} task(s). localOwner={isLocalOwner}");
    }

    public void NotifyTaskCompleted(int taskIndex)
    {
        // Any client (not just the TaskManager's owner) may report completion.
        MarkTaskCompleteServerRpc(taskIndex);
    }

    [ServerRpc(RequireOwnership = false)]
    private void MarkTaskCompleteServerRpc(int taskIndex)
    {
        if (taskIndex < 0 || taskIndex >= allTasks.Count)
        {
            Debug.LogWarning($"[TaskManager] Invalid task index {taskIndex} (server knows {allTasks.Count} tasks)");
            return;
        }

        int bit = 1 << taskIndex;
        if ((completedTasksMask.Value & bit) != 0) return; // already done

        completedTasksMask.Value |= bit;
        CheckAllComplete();
    }

    private void OnCompletedMaskChanged(int previous, int current)
    {
        int newlyCompleted = current & ~previous;

        if (isLocalSlugViewer)
            RefreshTaskListHud(current);

        // Trigger alarms everywhere for every newly-completed task.
        if (newlyCompleted != 0)
            TriggerAlarms(newlyCompleted);

        CheckAllComplete();
    }

    private void RefreshTaskListHud(int mask)
    {
        for (int i = 0; i < taskListItems.Count; i++)
        {
            if (taskListItems[i] == null) continue;
            bool done = (mask & (1 << i)) != 0;
            taskListItems[i].SetActive(!done);
        }
    }

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

    private void CheckAllComplete()
    {
        if (allDone || allTasks.Count == 0) return;

        int fullMask = (1 << allTasks.Count) - 1;
        if ((completedTasksMask.Value & fullMask) != fullMask) return;

        allDone = true;
        Debug.Log("[TaskManager] All tasks completed!");

        // Only invoke on the server — GameManager.EndGame calls
        // NetworkManager.SceneManager.LoadScene which is server-authoritative.
        if (IsServer)
            OnAllTasksCompleted?.Invoke();
    }

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
