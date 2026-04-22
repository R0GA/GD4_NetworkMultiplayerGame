using UnityEngine;
using UnityEngine.UI;

public class WorldHealthBar : MonoBehaviour
{
    [SerializeField] private Slider fill;
    [SerializeField] private NetworkHealth health;

    private void OnEnable()
    {
        if (health != null)
            health.Health.OnValueChanged += OnHealthChanged;

        UpdateFill();
    }

    private void OnDisable()
    {
        if (health != null)
            health.Health.OnValueChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int oldValue, int newValue)
    {
        UpdateFill();
    }

    private void UpdateFill()
    {
        if (!fill || health == null) return;
        fill.value = health.Health01;
    }
}
