using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class TaskManager : NetworkBehaviour
{
    [Header("Task Registry")]
    [Tooltip("All BaseTask components that exist in the scene. Populate in Inspector or let OnNetworkSpawn() find them.")]
    [SerializeField] private List<BaseTask> allTasks = new();

    [Header("UI")]
    [SerializeField] private GameObject taskListPanel;
    [SerializeField] private List<GameObject> taskListItems; // one per task, same order as allTasks

    public UnityEvent OnAllTasksCompleted = new();

    private readonly NetworkVariable<int> completedTasksMask = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private bool allDone = false;

    public override void OnNetworkSpawn()
    {
        if (allTasks.Count == 0)
            allTasks.AddRange(FindObjectsByType<BaseTask>(FindObjectsSortMode.None));

        for (int i = 0; i < allTasks.Count; i++)
            allTasks[i].Initialise(this, i);

        bool isOwner = IsOwner;
        if (taskListPanel) taskListPanel.SetActive(isOwner);

        completedTasksMask.OnValueChanged += OnCompletedMaskChanged;
    }

    public override void OnNetworkDespawn()
    {
        completedTasksMask.OnValueChanged -= OnCompletedMaskChanged;
    }

    // -------------------------------------------------------------------------
    // Called by individual tasks on the owning client
    // -------------------------------------------------------------------------

    public void NotifyTaskCompleted(int taskIndex)
    {
        if (!IsOwner) return;
        MarkTaskCompleteServerRpc(taskIndex);
    }

    // -------------------------------------------------------------------------
    // Server-side logic
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
    // Reacts to the network variable changing on ALL clients
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

        // Trigger room lights and ship alarm for each newly completed task.
        // This runs on every client, so both players get lights and audio.
        if (newlyCompleted != 0)
            TriggerAlarms(newlyCompleted);

        CheckAllComplete();
    }

    // -------------------------------------------------------------------------
    // Alarm & light helpers — run on every client
    // -------------------------------------------------------------------------

    private void TriggerAlarms(int newlyCompletedMask)
    {
        // Activate room warning lights on the matching TaskTrigger.
        var allTriggers = FindObjectsByType<TaskTrigger>(FindObjectsSortMode.None);
        for (int i = 0; i < allTasks.Count; i++)
        {
            if ((newlyCompletedMask & (1 << i)) == 0) continue;

            string id = allTasks[i].taskIdentifier;
            foreach (var trigger in allTriggers)
            {
                if (trigger.TaskIdentifier == id)
                    trigger.ActivateAlarmLights();
            }
        }

        // Delegate audio to the scene-level ShipAudioManager so it plays
        // reliably on every client from a non-networked, always-present object.
        if (ShipAudioManager.Instance != null)
            ShipAudioManager.Instance.PlayAlarm();
        else
            Debug.LogWarning("[TaskManager] ShipAudioManager.Instance is null — place a ShipAudioManager component on a persistent ship GameObject.");
    }

    // -------------------------------------------------------------------------
    // Completion check
    // -------------------------------------------------------------------------

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

    // -------------------------------------------------------------------------
    // Utilities
    // -------------------------------------------------------------------------

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