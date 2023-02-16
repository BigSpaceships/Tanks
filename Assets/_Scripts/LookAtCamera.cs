using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;

public class LookAtCamera : MonoBehaviour {
    private void Update() {
        transform.rotation = quaternion.identity;

        var cameraPos = Camera.main.transform.position;
        var relativeVector = cameraPos - transform.position;
        
        transform.Rotate(Vector3.up, -Mathf.Atan2(relativeVector.z, relativeVector.x) * Mathf.Rad2Deg - 90);
    }
}