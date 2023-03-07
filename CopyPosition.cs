using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CopyPosition : MonoBehaviour
{
    [SerializeField]
    private bool x, y, z;

    public Transform target;

    void Update()
    {
        transform.position = new Vector3(
            (x ? target.position.x : target.position.x),
            (y ? target.position.y : target.position.y),
            (z ? target.position.z : target.position.z));
    }
}
