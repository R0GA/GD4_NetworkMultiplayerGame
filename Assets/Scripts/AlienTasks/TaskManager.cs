using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

public class TaskManager : MonoBehaviour
{
    private EngineContoller engineContoller;
    private ReactorSabotage reactorSabotage;


    [Header("UI")]
    public GameObject engineTxt;
    public GameObject reactorTxt;
    public GameObject foodText;


    private FoodDestroyScript foodTask;
    private ReactorSabotage reactorTask;
    private EngineContoller engineTask;

    public bool slugwins;

    // Event triggered when all tasks are completed
    public UnityEvent OnAllTasksCompleted = new UnityEvent();
    private bool tasksCompleted = false;

    // Update is called once per frame


    private void Start()
    {
        foodTask = GetComponent<FoodDestroyScript>();
        reactorTask = GetComponent<ReactorSabotage>();
        engineTask = GetComponent<EngineContoller>();
    }
    void Update()
    {
        if(engineContoller.isEngineDestroyed)

        {
            engineTxt.SetActive(false);
        }


        if (reactorSabotage.isReactorDestroyed)
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
