using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

public class TaskManager : MonoBehaviour
{
   


    [Header("UI")]
    public GameObject engineTxt;
    public GameObject reactorTxt;
    public GameObject foodText;


    [SerializeField] private FoodDestroyScript foodTask;
    [SerializeField] private ReactorSabotage reactorTask;
    [SerializeField]private EngineContoller engineTask;

    public bool slugwins;

    // Event triggered when all tasks are completed
    public UnityEvent OnAllTasksCompleted = new UnityEvent();
    private bool tasksCompleted = false;

    // Update is called once per frame

    private void Start()
    {
        foodTask = FindAnyObjectByType<FoodDestroyScript>();
        reactorTask = FindAnyObjectByType<ReactorSabotage>();
        engineTask = FindAnyObjectByType<EngineContoller>();
    }

    void Update()
    {
        if(engineTask.isEngineDestroyed)

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
