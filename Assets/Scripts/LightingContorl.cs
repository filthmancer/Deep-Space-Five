using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightingContorl : MonoBehaviour
{
    public Light directionalLightMain;
    public float sunTurnRate = -0.5F;
    void Update()
    {
        directionalLightMain.transform.Rotate(0, sunTurnRate * Time.deltaTime, 0, Space.Self);
    }
}
