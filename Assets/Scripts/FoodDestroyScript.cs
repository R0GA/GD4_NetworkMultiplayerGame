using UnityEngine;

public class FoodDestroyScript : MonoBehaviour
{
    [SerializeField] private float foodCount;
    [SerializeField] private float totalFoodDestroyed;
    public ParticleSystem binFlames;
    public bool foodDestroyed;

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Food"))
        {
            foodCount++;
            Destroy(other);
        }
    }

    public void Update()
    {
        if (foodCount >= totalFoodDestroyed)
        {
            foodDestroyed = true;
           
        }
    }
}
