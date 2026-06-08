using UnityEngine;

public class RoomNameRotation : MonoBehaviour
{
    public Transform player;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindWithTag("Astronaut").transform;
    }

    // Update is called once per frame
    void Update()
    {
        transform.rotation = player.rotation;
    }
}
