using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReactorSabotage : MonoBehaviour
{
    [Header("UI References")]
    public Slider fillBar;
    public Button clickButton;
    //public Button resetButton;
    //public TMP_Text valueText;
    //public TMP_Text clicksText;
    //public TMP_Text deltaText;

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
    public bool reactorDestroyed;


    void Start()
    {
        // Configure slider range
        fillBar.minValue = 0f;
        fillBar.maxValue = maxValue;
        fillBar.interactable = false; // Player shouldn't drag it manually

        clickButton.onClick.AddListener(OnClickButton);

        //if (resetButton != null)
        //    resetButton.onClick.AddListener(ResetGauge);

        RefreshUI(0f);
        //deltaText.text = "Click the button to start!";
    }

    public void OnClickButton()
    {
        if (isAnimating) return;
        totalClicks++;
        StartCoroutine(AnimateGauge());
    }

    IEnumerator AnimateGauge()
    {
        isAnimating = true;

        float startValue = currentValue;
        float afterAdd = Mathf.Min(startValue + addAmount, maxValue);
        float afterSub = Mathf.Max(afterAdd - subtractAmount, 0f);

        // Phase 1 — rise by +5
        //deltaText.text = "+5 added...";
        yield return StartCoroutine(AnimateBar(startValue, afterAdd, riseTime));

        yield return new WaitForSeconds(holdTime);

        // Phase 2 — drop by -2
        //deltaText.text = "+5 then −2 = net +3";
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
            float ease = 1f - Mathf.Pow(1f - t, 3f); // ease-out cubic
            fillBar.value = Mathf.Lerp(from, to, ease);
            yield return null;
        }

        fillBar.value = to;
    }

    void RefreshUI(float val)
    {
        fillBar.value = val;
        //valueText.text = Mathf.RoundToInt(val).ToString();
        //clicksText.text = totalClicks.ToString();
    }

    void ResetGauge()
    {
        if (isAnimating)
        {
            StopAllCoroutines();
            isAnimating = false;
        }

        currentValue = 0f;
        totalClicks = 0;
        RefreshUI(0f);
        //deltaText.text = "Click the button to start!";
    }


}