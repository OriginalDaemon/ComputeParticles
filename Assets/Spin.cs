using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spin : MonoBehaviour
{
    public float speedDegrees = 10.0f;

    // Update is called once per frame
    void FixedUpdate()
    {
        transform.rotation = Quaternion.AngleAxis(speedDegrees * Time.fixedDeltaTime, Vector3.up) * transform.rotation;
    }
}
