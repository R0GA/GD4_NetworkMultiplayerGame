using UnityEngine;
using UnityEngine.Events;

public abstract class BaseTask : MonoBehaviour
{
   
    [Header("Base Task")]
    [SerializeField] protected GameObject taskPanel;
    [SerializeField] public string taskDisplayName = "Task";

    [Tooltip("Unique ID that matches the TaskTrigger's taskIdentifier.")]
    [SerializeField] public string taskIdentifier = "";


    public UnityEvent OnTaskCompleted = new();


    protected TaskManager taskManager;
    protected SlugPlayer currentPlayer;
    protected int taskIndex = -1;
    private bool isComplete = false;
    private bool isOpen = false;

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

        // Lock player movement / camera
        if (currentPlayer) currentPlayer.SetUIMode(true);

        OnOpen();
        return true;
    }

    public void CloseTask()
    {
        if (!isOpen) return;
        isOpen = false;

        if (taskPanel) taskPanel.SetActive(false);

        if (currentPlayer) currentPlayer.SetUIMode(false);

        OnClose();
    }

 
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


    protected abstract void OnOpen();

    protected virtual void OnClose() { }
    public bool IsComplete => isComplete;
    public bool IsOpen => isOpen;
}
