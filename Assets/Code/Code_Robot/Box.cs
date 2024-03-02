using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Box : MonoBehaviour
{
    private Vector3 _InitPos = Vector3.zero;
    private Vector3 _InitRotation = Vector3.zero;

    private void Awake()
    {
        _InitPos = transform.position;
        _InitRotation = transform.eulerAngles;
    }
    
    public void ResetBox()
    {
        transform.position = _InitPos;
        transform.rotation = Quaternion.Euler(_InitRotation);
    }
}
