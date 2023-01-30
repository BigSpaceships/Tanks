using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TankInput : MonoBehaviour {
    public Animator animator;

    private float _horizontalInput;
    private float _verticalInput;
    
    private void Update() {

        animator.SetFloat("Vertical", _verticalInput);
        animator.SetFloat("Horizontal", _horizontalInput);

        if (Math.Abs(_horizontalInput) < .01 && Math.Abs(_verticalInput) < .01) {
            animator.SetBool("Idle", true);
        }
        else {
            animator.SetBool("Idle", false);
        }
    }
    
    public void OnMove(InputAction.CallbackContext context) {
        _verticalInput = context.ReadValue<Vector2>().y;
        _horizontalInput = context.ReadValue<Vector2>().x;
    }
}
