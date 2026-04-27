using UnityEngine;

public class FoodDestroyScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private float foodCount;
    [SerializeField] private float totalFoodDestroyed;
    public ParticleSystem binFlames;
    public bool foodDestroyed;



    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Food"))
        {
            foodCount++;

        }
    }



    public void Update()
    {
        if (foodCount >= totalFoodDestroyed)
        {
            foodDestroyed = true;
            binFlames.Play();
        }
    }
}
