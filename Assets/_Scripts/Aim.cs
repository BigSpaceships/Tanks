using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Aim : MonoBehaviour {
    [SerializeField] private GameObject aimObject;
    [SerializeField] private int numberOfPoints;

    private GameObject _focusedTank;

    private float _launchAngle;
    private float _yawAngle;
    private float _pathLength;
    [SerializeField] private float launchSpeed;

    void Update() {
        _focusedTank = GameManager.GetFocusedTank();

        var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out var hit)) {
            if (_focusedTank == null) return;

            _focusedTank.GetComponent<TankData>().UpdateTargetPosition(hit.point);
        }

        CalculatePathValues();
    }

    private void CalculatePathValues() {
        if (_focusedTank == null) return;

        var gravityValue = -Physics.gravity.y;

        var relativeVector =
            _focusedTank.GetComponent<TankData>().GetTargetPosition() - _focusedTank.transform.position;

        var horizontalDistance = new Vector3(relativeVector.x, 0, relativeVector.z).magnitude;

        _launchAngle = (float)Math.Atan(
            (launchSpeed * launchSpeed - Math.Sqrt(Math.Pow(launchSpeed, 4) - gravityValue *
                (gravityValue * horizontalDistance * horizontalDistance +
                 2 * relativeVector.y * launchSpeed * launchSpeed))) / (gravityValue * horizontalDistance));

        _yawAngle = (float)Math.Atan2(relativeVector.x, relativeVector.z);
    }

    void DrawPath() {
        if (_focusedTank == null) return;

        var targetPos = _focusedTank.GetComponent<TankData>().GetTargetPosition();

        var points = new Vector3[numberOfPoints];

        for (int i = 0; i < numberOfPoints; i++) {
            // var horizontalDist = launchSpeed * 
        }
    }
}