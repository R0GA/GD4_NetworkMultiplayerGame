using System;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UI;

public class ReactorSabotage : MonoBehaviour
{
    [Header("Value Settings")]
    [SerializeField] private float maxAmount = 100f;
    [SerializeField] private float addAmount = 5f;
    [SerializeField] private float subtractAmount = 2f;
    [SerializeField] private float currentAmount;
    private float StartValue = 0;

    [Header("UI Settings")]
    public Slider fillbar;

    void Start()
    {
        currentAmount = 0;
        fillbar.minValue = 0;
        fillbar.maxValue = maxAmount;
        
    }

    void Update()
    {
        fillbar.value = currentAmount;
       
    }

    public void ButtonClick()
    {

       
        float startValue = currentAmount;
        float afterAdd = Mathf.Min(startValue + addAmount, maxAmount);
        float afterSub = Mathf.Max(afterAdd - subtractAmount, 0f);

        currentAmount = afterSub;

        print(currentAmount.ToString());
      
    }
}