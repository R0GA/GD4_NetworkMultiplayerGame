using UnityEngine;
using UnityEngine.Rendering.Universal;

public class TaskManager : MonoBehaviour
{
    private EngineContoller engineContoller;
    private ReactorSabotage reactorSabotage;


    [Header("UI")]
    public GameObject engineTxt;
    public GameObject reactorTxt;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(engineContoller.isEngineDestroyed)

        {
            engineTxt.SetActive(false);
        }


        if(reactorSabotage.isReactorDestroyed)
        {
            reactorTxt.SetActive(false);
        }
    }
}
