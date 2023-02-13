using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class TankController : NetworkBehaviour {
    public InputActionMap controls;

    [SerializeField] private List<WheelCollider> leftWheels;
    [SerializeField] private List<WheelCollider> rightWheels;

    [SerializeField] private float motorForce;
    [SerializeField] private float turnForce;
    [SerializeField] private float breakForce;

    [SerializeField] private float turnStopLimit;
    [SerializeField] private float turnStopSpeed;

    private float _forwardInput;
    private float _turnInput;

    private Rigidbody _rb;

    private void Awake() {
        _rb = GetComponent<Rigidbody>();
    }

    private void OnEnable() {
        UpdateControls();
    }

    public override void OnNetworkSpawn() {
        UpdateControls();
    }

    private void Update() {
        if (NetworkManager.Singleton.IsServer) {
            Move();
        }
    }

    private void Move() {
        var currentTurnRate = _rb.angularVelocity.y;

        var turnForceScale = _turnInput;

        if (Mathf.Abs(currentTurnRate) > turnStopLimit && Mathf.Abs(_turnInput) <= .1) {
            turnForceScale = -Math.Sign(currentTurnRate) * turnStopSpeed;
        }

        foreach (var wheel in leftWheels) {
            wheel.motorTorque = motorForce * _forwardInput + turnForceScale * turnForce;

            if (_forwardInput == 0 && _turnInput == 0) {
                wheel.brakeTorque = breakForce;
            }
            else {
                wheel.brakeTorque = 0;
            }

            ApplyVisualsToWheel(wheel);
        }

        foreach (var wheel in rightWheels) {
            wheel.motorTorque = motorForce * _forwardInput - turnForceScale * turnForce;

            if (_forwardInput == 0 && _turnInput == 0) {
                wheel.brakeTorque = breakForce;
            }
            else {
                wheel.brakeTorque = 0;
            }

            ApplyVisualsToWheel(wheel);
        }
    }

    private void UpdateControls() {
        if (NetworkManager.Singleton.IsClient) {
            Debug.Log("controls");
            if (NetworkObject.IsOwner) {
                controls.Enable();

                controls["Move"].performed += OnMove;
                controls["Move"].canceled += OnMove;
            }
        }
    }

    private void ApplyVisualsToWheel(WheelCollider wheel) {
        if (wheel.transform.childCount == 0) {
            return;
        }

        var wheelRendererTransform = wheel.transform.GetChild(0);

        wheel.GetWorldPose(out var position, out var rotation);

        wheelRendererTransform.position = position;
        wheelRendererTransform.rotation = rotation;
    }

    private void OnMove(InputAction.CallbackContext context) {
        if (!NetworkManager.Singleton.IsClient) return;

        if (!NetworkObject.IsOwner) return;

        _forwardInput = context.ReadValue<Vector2>().y;
        _turnInput = context.ReadValue<Vector2>().x;

        SendMovementInputServerRpc(context.ReadValue<Vector2>());
    }

    [ServerRpc]
    private void SendMovementInputServerRpc(Vector2 input) {
        _forwardInput = input.y;
        _turnInput = input.x;
    }
}