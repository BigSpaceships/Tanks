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
    private float _tEnd;
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
        
        _lineRenderer.material.SetFloat("_Length", _pathLength);
        
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

        _tEnd = horizontalDistance / launchSpeed / Mathf.Cos(_launchAngle);

        _pathLength = Util.Integrate((float x) => Mathf.Sqrt(Mathf.Pow(launchSpeed * Mathf.Cos(_launchAngle), 2) +
            Mathf.Pow(-_gravityValue * x + launchSpeed * Mathf.Sin(_launchAngle), 2)), 0, _tEnd, 100);

        _yawAngle = Mathf.Atan2(relativeVector.x, relativeVector.z);
    }

    void DrawPath() {
        if (_focusedTank == null) return;

        var points = new Vector3[numberOfPoints];

        if (float.IsNaN(_launchAngle)) {
            _lineRenderer.positionCount = numberOfPoints;
            _lineRenderer.SetPositions(points);

            return;
        }

        for (int i = 0; i < numberOfPoints; i++) {
            var t = i * _tEnd / numberOfPoints;

            var horizontalDist = launchSpeed * t * Mathf.Cos(_launchAngle);

            var xDist = Mathf.Sin(_yawAngle) * horizontalDist;
            var zDist = Mathf.Cos(_yawAngle) * horizontalDist;

            var yDist = -.5f * _gravityValue * t * t + launchSpeed * t * Mathf.Sin(_launchAngle);

            var posChange = new Vector3(xDist, yDist, zDist);

            points[i] = _focusedTank.transform.position + posChange;
        }

        _lineRenderer.positionCount = numberOfPoints;
        _lineRenderer.SetPositions(points);
    }
}