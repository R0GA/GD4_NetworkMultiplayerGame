
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
    public bool isReactorCritical;

    [Header("UI Settings")]
    public Slider fillbar;

    [Header("Animation")]
    public float riseTime = 0.25f;
    public float holdTime = 0.15f;
    public float dropTime = 0.18f;

    void Start()
    {
        currentAmount = 0;
        fillbar.minValue = 0;
        fillbar.maxValue = maxAmount;
        
    }

    void Update()
    {
        fillbar.value = currentAmount;

        if (fillbar.value > 60f )
        {
            fillbar.image.color = Color.orange;
        }

        if (fillbar.value > 90f)
        {
            fillbar.image.color= Color.red;
        }

        if (currentAmount ==98f)
        {
            currentAmount += 2;
        }


        if (currentAmount ==100f)
        {
            isReactorCritical = true;
        }

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