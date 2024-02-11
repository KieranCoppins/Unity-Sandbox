using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSAverage : MonoBehaviour
{
    float averageMS = 0;

    void Update()
    {
        averageMS += ((Time.deltaTime / Time.timeScale) - averageMS) * 0.03f;
        Debug.Log(1f / averageMS);
    }
}
