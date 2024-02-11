using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroySelfAfterTime : MonoBehaviour
{
    [SerializeField] private float time;

    // Start is called before the first frame update
    void Awake()
    {
        StartCoroutine(DestorySelf());
    }

    // Update is called once per frame
    IEnumerator DestorySelf()
    {
        yield return new WaitForSeconds(time);
        Destroy(gameObject);
    }
}
