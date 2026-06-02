using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ReactorSabotage : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject canvas;
    public Slider fillBar;
    public Button clickButton;
   
    [Header("Gauge Settings")]
    public float maxValue = 100f;
    public float addAmount = 5f;
    public float subtractAmount = 2f;

    [Header("Animation")]
    public float riseTime = 0.25f;
    public float holdTime = 0.15f;
    public float dropTime = 0.18f;

    private float currentValue = 0f;
    private int totalClicks = 0;
    private bool isAnimating = false;
    public bool isReactorDestroyed;

    private SeeReactor seeReactor;


    void Start()
    {
        //canvas = GameObject.FindWithTag("ReactorCanvas");   
        //fillBar = canvas.GetComponentInChildren<Slider>();
        //clickButton = canvas.GetComponentInChildren<Button>();

        //canvas.SetActive(false);

        fillBar.minValue = 0f;
        fillBar.maxValue = maxValue;
        fillBar.interactable = false;

        //clickButton.onClick.AddListener(OnClickButton);

        seeReactor = GetComponentInChildren<SeeReactor>();

        RefreshUI(0f);
      
    }

    public void OnClickButton()
    {
        if (isAnimating) return;
      
        StartCoroutine(AnimateGauge());
    }

    IEnumerator AnimateGauge()
    {
        isAnimating = true;

        float startValue = currentValue;
        float afterAdd = Mathf.Min(startValue + addAmount, maxValue);
        float afterSub = Mathf.Max(afterAdd - subtractAmount, 0f);

      
        yield return StartCoroutine(AnimateBar(startValue, afterAdd, riseTime));

        yield return new WaitForSeconds(holdTime);

        yield return StartCoroutine(AnimateBar(afterAdd, afterSub, dropTime));

        currentValue = afterSub;
        RefreshUI(currentValue);

        

        isAnimating = false;
    }

    IEnumerator AnimateBar(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = 1f - Mathf.Pow(1f - t, 3f); 
            fillBar.value = Mathf.Lerp(from, to, ease);
            yield return null;
        }

        fillBar.value = to;
    }

    void RefreshUI(float val)
    {
        fillBar.value = val;
       
    }

    private void Update()
    {
        if(currentValue >=98f && !isReactorDestroyed)
        {
            isReactorDestroyed = true;
            StartCoroutine(BlowReactor()); 
        }
    }

    IEnumerator BlowReactor()

     {
        yield return new WaitForSeconds(1);
        canvas.SetActive(false);
        seeReactor.taskCompleted = true;
        seeReactor.Close();
    }

}