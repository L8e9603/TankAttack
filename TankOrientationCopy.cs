using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankOrientationCopy : MonoBehaviour
{
    [SerializeField]
    private bool x, y, z;

    [SerializeField]
    private Transform tankBodyTransform;

    public Transform target;

    void Update()
    {
        if (target == null) return;

        tankBodyTransform.eulerAngles = new Vector3(0, -(y ? target.localEulerAngles.y : target.localEulerAngles.y), 0);
    }
}
