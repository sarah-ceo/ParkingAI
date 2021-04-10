using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class CameraTracker : MonoBehaviour
{
    public GameObject trackedObject;
    [Range(1.0f, 20.0f)]
    public float height = 10.0f;

    void Start()
    {
        Assert.IsNotNull(trackedObject);
    }

    void Update()
    {
        Vector3 pos = trackedObject.transform.position;
        pos.y = height;
        this.transform.position = pos;
    }
}
