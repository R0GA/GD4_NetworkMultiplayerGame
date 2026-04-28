using System.Collections;
using UnityEngine;

public class EngineContoller : MonoBehaviour
{
  [Header("UI Display")]
  public GameObject engineDestroyedText;
  public GameObject engineCriticalText;

    [Header("VfX")]
   public ParticleSystem engineFire;
   public ParticleSystem engineExplode;

    public GameObject engine;
    public Canvas engineCameraCanvas;
    public float engineRotationAmount ;
    private SeeEngineCamera seeEngine;
    
    public float engineKillCountTime; 

    public bool isEngineCorrect = false;

    public bool isEngineDestroyed;


    public void Start()
    {
        seeEngine = GetComponentInChildren<SeeEngineCamera>();
    }

    public void EngineUp()
    {
        engine.transform.Rotate(0f, 0f, engineRotationAmount);
    }

    public void EngineDown()
    {
        engine.transform.Rotate(0f,0f,-engineRotationAmount);
    }

    public void EngineCorrect()
    {
        if (isEngineCorrect)
        {
            engineCriticalText.SetActive(true);
            //Debug.Log("Engine Danger");
        }
        else if(!isEngineCorrect)
        {
            engineCriticalText.SetActive(false);
        }
       
    }

    public void EngineFire()
    {
        if (isEngineCorrect)
        {
            engineFire.Play();
            StartCoroutine(EngineKillCountDown());
        }
    }

   public IEnumerator EngineKillCountDown()

    {

    yield return new WaitForSeconds(engineKillCountTime);

        EngineKaboom();
        engineFire.Stop();
        engineExplode.Play();

        yield return new WaitForSeconds(1);
        engineCameraCanvas.enabled = false;
    }

    public void EngineKaboom()
    {
        engineDestroyedText.SetActive(true);
        Debug.Log("Engine GoKAboom");
        isEngineDestroyed = true;
        seeEngine.taskCompleted = true;
        seeEngine.Close();
    }

    

    public void FixedUpdate()
    {
      

        if (engine.transform.eulerAngles.z >267&&engine.transform.eulerAngles.z <273f)

        {
            isEngineCorrect = true;
            //print("EngineCorrect");
            EngineCorrect();
        }

        else if(engine.transform.eulerAngles.z >273 || engine.transform.eulerAngles.z < 267)
        {
            isEngineCorrect= false;
            //print("Engine Wrong");
           engineCriticalText?.SetActive(false);
        }

    }
}
