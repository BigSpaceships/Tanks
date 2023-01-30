using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {
    public GameObject tank;

    private Vector3 offset;

    private void Start() {
        offset = transform.position - tank.transform.position;
    }

    private void Update() {
        transform.position = tank.transform.position + offset;
    }
}
