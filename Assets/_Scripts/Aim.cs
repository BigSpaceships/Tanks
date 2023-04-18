using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Aim : MonoBehaviour {
    [SerializeField] private GameObject aimObject;
    [SerializeField] private int numberOfPoints;

    private GameObject _focusedTank;
    private LineRenderer _lineRenderer;

    [SerializeField] private float _launchAngle;
    private float _yawAngle;
    private float _pathLength;
    private float _gravityValue;
    
    [SerializeField] private float launchSpeed;

    private void Start() {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    void Update() {
        _focusedTank = GameManager.GetFocusedTank();

        var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out var hit)) {
            if (_focusedTank == null) return;

            _focusedTank.GetComponent<TankData>().UpdateTargetPosition(hit.point);
        }

        CalculatePathValues();
        
        DrawPath();
    }

    private void CalculatePathValues() {
        if (_focusedTank == null) return;

        _gravityValue = -Physics.gravity.y;

        var relativeVector =
            _focusedTank.GetComponent<TankData>().GetTargetPosition() - _focusedTank.transform.position;

        var horizontalDistance = new Vector3(relativeVector.x, 0, relativeVector.z).magnitude;

        _launchAngle = Mathf.Atan(
            (launchSpeed * launchSpeed - Mathf.Sqrt(Mathf.Pow(launchSpeed, 4) - _gravityValue *
                (_gravityValue * horizontalDistance * horizontalDistance +
                 2 * relativeVector.y * launchSpeed * launchSpeed))) / (_gravityValue * horizontalDistance));

        _yawAngle = (float)Mathf.Atan2(relativeVector.x, relativeVector.z);
    }

    void DrawPath() {
        if (_focusedTank == null) return;

        var points = new Vector3[numberOfPoints];

        for (int i = 0; i < numberOfPoints; i++) {
            var t = i / (float) (numberOfPoints - 1);

            var horizontalDist = launchSpeed * t * Mathf.Cos(_launchAngle);

            var xDist = Mathf.Sin(_yawAngle) * horizontalDist;
            var zDist = Mathf.Cos(_yawAngle) * horizontalDist;

            var yDist = -.5f * _gravityValue * t * t + launchSpeed * t * Mathf.Sin(_launchAngle);

            var posChange = new Vector3(xDist, yDist, zDist);

            points[i] = _focusedTank.transform.position + posChange;
        }
        
        _lineRenderer.SetPositions(points);
    }
}