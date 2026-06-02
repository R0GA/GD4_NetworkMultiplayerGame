using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;


public class TaskManager : NetworkBehaviour
{
    [Header("Task Registry")]
    [Tooltip("All BaseTask components that exist in the scene. Populate in Inspector or let Start() find them.")]
    [SerializeField] private List<BaseTask> allTasks = new();

    [Header("UI")]
    [SerializeField] private GameObject taskListPanel;   // optional HUD showing task list
    [SerializeField] private List<GameObject> taskListItems; // one per task, same order as allTasks


    public UnityEvent OnAllTasksCompleted = new();

    private readonly NetworkVariable<int> completedTasksMask = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private bool allDone = false;

    public override void OnNetworkSpawn()
    {
        // Auto-discover tasks if none assigned
        if (allTasks.Count == 0)
            allTasks.AddRange(FindObjectsByType<BaseTask>(FindObjectsSortMode.None));

        // Register each task with an index and hook completion callback
        for (int i = 0; i < allTasks.Count; i++)
        {
            int capturedIndex = i;
            allTasks[i].Initialise(this, capturedIndex);
        }

        // UI is owner-only
        bool isOwner = IsOwner;
        if (taskListPanel) taskListPanel.SetActive(isOwner);

        // Subscribe to network changes so non-owner clients (seeker) can react
        completedTasksMask.OnValueChanged += OnCompletedMaskChanged;
    }

    public override void OnNetworkDespawn()
    {
        completedTasksMask.OnValueChanged -= OnCompletedMaskChanged;
    }

    public void NotifyTaskCompleted(int taskIndex)
    {
        if (!IsOwner) return;
        MarkTaskCompleteServerRpc(taskIndex);
    }

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

    private void OnCompletedMaskChanged(int previous, int current)
    {
        // Update task list HUD items for owner
        if (IsOwner)
        {
            for (int i = 0; i < taskListItems.Count; i++)
            {
                if (taskListItems[i] == null) continue;
                bool done = (current & (1 << i)) != 0;
                // You can add a CanvasGroup fade or strikethrough here
                taskListItems[i].SetActive(!done);
            }
        }

        // Check completion (runs on all clients so each can react)
        CheckAllComplete();
    }

    private void CheckAllComplete()
    {
        if (allDone || allTasks.Count == 0) return;

        int fullMask = (1 << allTasks.Count) - 1;
        if ((completedTasksMask.Value & fullMask) == fullMask)
        {
            allDone = true;
            Debug.Log("[TaskManager] All tasks completed!");
            OnAllTasksCompleted?.Invoke();
        }
    }

    public bool IsTaskComplete(int index) =>
        index >= 0 && index < allTasks.Count && (completedTasksMask.Value & (1 << index)) != 0;

    public int CompletedCount()
    {
        int count = 0;
        int mask = completedTasksMask.Value;
        while (mask != 0) { count += mask & 1; mask >>= 1; }
        return count;
    }
}
