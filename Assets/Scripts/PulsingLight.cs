using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PulsingLight : MonoBehaviour
{
    private Light lightComponent;
    [SerializeField]
    private float pulseMax = 10, pulseMin = 0;
    [SerializeField]
    private float speed = 1.0F;
    // Start is called before the first frame update
    void Start()
    {
        if (lightComponent == null)
        {
            lightComponent = this.transform.GetComponent<Light>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        lightComponent.intensity = pulseMin + Mathf.PingPong(Time.time * speed, pulseMax - pulseMin);
    }
}
