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


    [SerializeField] private FoodDestroyScript foodTask;
    [SerializeField] private ReactorSabotage reactorTask;
    [SerializeField]private EngineContoller engineTask;

    public bool slugwins;

    // Event triggered when all tasks are completed
    public UnityEvent OnAllTasksCompleted = new UnityEvent();
    private bool tasksCompleted = false;

    // Update is called once per frame



      
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
