using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class TaskManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject engineTxt;
    public GameObject reactorTxt;
    public GameObject foodText;
    public Canvas canvas;

    [SerializeField] private FoodDestroyScript foodTask;
    [SerializeField] private ReactorSabotage reactorTask;
    [SerializeField] private EngineContoller engineTask;
    private NetworkObject networkObject;    

    public bool slugwins;

    public UnityEvent OnAllTasksCompleted = new UnityEvent();
    private bool tasksCompleted = false;


    private void Start()
    {
        foodTask = FindAnyObjectByType<FoodDestroyScript>();
        reactorTask = FindAnyObjectByType<ReactorSabotage>();
        engineTask = FindAnyObjectByType<EngineContoller>();

        networkObject = GetComponentInParent<NetworkObject>();

        if (!networkObject.IsOwner) 
            canvas.enabled = false;

        canvas.enabled = true;

    }
    void Update()
    {

        if (!foodTask || !reactorTask || !engineTask)
        {
            foodTask = FindAnyObjectByType<FoodDestroyScript>();
            reactorTask = FindAnyObjectByType<ReactorSabotage>();
            engineTask = FindAnyObjectByType<EngineContoller>();
        }

        if (engineTask.isEngineDestroyed)
        {
            engineTxt.SetActive(false);
        }
        if (reactorTask.isReactorDestroyed)
        {
            reactorTxt.SetActive(false);
        }
        if (foodTask.foodDestroyed)
        {
            foodText.SetActive(false);
        }
        ATDone();
    }
    public void ATDone()
    {
        if(foodTask.foodDestroyed && engineTask.isEngineDestroyed && reactorTask.isReactorDestroyed)
        {
            if (!tasksCompleted)
            {
                tasksCompleted = true;
                OnAllTasksCompleted?.Invoke();
            }
        }
    }
}
