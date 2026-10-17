using UnityEngine;

// Makes a light twitch/flicker unevenly, used as the visual tell that we've slipped
// into the dream. Starts disabled -- SleepInteract enables it once the dream begins.
public class ChandelierFlicker : MonoBehaviour
{
    public float minMultiplier = 0.05f;
    public float maxMultiplier = 1.8f;
    public float minHoldTime = 0.02f;
    public float maxHoldTime = 0.12f;

    private Light[] lights;
    private float[] baseIntensities;
    private float timer;

    void Awake()
    {
        lights = GetComponentsInChildren<Light>();
        baseIntensities = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++) baseIntensities[i] = lights[i].intensity;
        enabled = false;
    }

    void OnEnable()
    {
        timer = 0f;
    }

    void OnDisable()
    {
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null) lights[i].intensity = baseIntensities[i];
        }
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            float mult = Random.Range(minMultiplier, maxMultiplier);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null) lights[i].intensity = baseIntensities[i] * mult;
            }
            timer = Random.Range(minHoldTime, maxHoldTime);
        }
    }
}
