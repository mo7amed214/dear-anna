using UnityEngine;

// Drives a background street vehicle continuously along the world X axis, then snaps back
// to the start and does it again -- so it always looks like it's arriving, never reversing,
// and there's a steady stream of traffic passing the window.
public class StreetVehicleLoop : MonoBehaviour
{
    public float speed = 1.5f;
    public float range = 26f; // distance traveled before looping back to the start
    public bool reverseDirection = false; // flip which way is "forward" without touching rotation

    private Vector3 startPosition;
    private Vector3 forwardDir;
    private float traveled;

    void Awake()
    {
        startPosition = transform.position;
        forwardDir = Vector3.right * (reverseDirection ? -1f : 1f); // world X axis, not local
        traveled = 0f;
    }

    void Update()
    {
        traveled += speed * Time.deltaTime;
        if (traveled >= range) traveled -= range;

        transform.position = startPosition + forwardDir * traveled;
    }
}
