using UnityEngine;
using UnityEngine.Rendering.Universal;

public class TaskManager : MonoBehaviour
{
    private EngineContoller engineContoller;
    private ReactorSabotage reactorSabotage;


    [Header("UI")]
    public GameObject engineTxt;
    public GameObject reactorTxt;


    private FoodDestroyScript foodTask;
    private ReactorSabotage reactorTask;
    private EngineContoller engineTask;

    public bool slugwins;

    // Update is called once per frame
    void Update()
    {
        if(engineContoller.isEngineDestroyed)

        {
            engineTxt.SetActive(false);
        }


        //if (reactorSabotage.isReactorDestroyed)
        //{
        //    reactorTxt.SetActive(false);
        //}






        ATDone();
    }






    public void ATDone()
    {
        if(foodTask.foodDestroyed && engineTask.isEngineDestroyed && reactorTask.reactorDestroyed)
        {

        }
    }
}
