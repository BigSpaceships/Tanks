using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Aim : MonoBehaviour {
    [SerializeField] private GameObject aimObject;
    [SerializeField] private int numberOfPoints;

    private GameObject _focusedTank;
    private TankParts _tankParts;
    private LineRenderer _lineRenderer;

    private float _launchAngle;
    private float _yawAngle;
    private float _pathLength;
    private float _tEnd;
    private float _horizontalDistance;
    private float _yDistance;

    private float _gravityValue;

    [SerializeField] private float launchSpeed;
    public float barrelLength;

    private void Start() {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    void Update() {
        _focusedTank = GameManager.GetFocusedTank();

        if (_focusedTank != null) {
            _tankParts = _focusedTank.GetComponent<TankParts>();
        }
        else {
            _tankParts = null;
        }

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

        var tankData = _focusedTank.GetComponent<TankData>();

        var relativeVector =
            tankData.GetTargetPosition() - _tankParts.barrel.transform.position;

        _horizontalDistance = new Vector3(relativeVector.x, 0, relativeVector.z).magnitude;
        _yDistance = relativeVector.y;

        _launchAngle = Mathf.Atan(
            (launchSpeed * launchSpeed - Mathf.Sqrt(Mathf.Pow(launchSpeed, 4) - _gravityValue *
                (_gravityValue * _horizontalDistance * _horizontalDistance +
                 2 * relativeVector.y * launchSpeed * launchSpeed))) / (_gravityValue * _horizontalDistance));

        _launchAngle = ImproveGuess(_launchAngle);
        _launchAngle = ImproveGuess(_launchAngle);
        _launchAngle = ImproveGuess(_launchAngle);

        _tEnd = _horizontalDistance / launchSpeed / Mathf.Cos(_launchAngle);

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
        
        _tankParts.barrel.transform.localRotation = Quaternion.Euler(-_launchAngle * Mathf.Rad2Deg, 0, 0);
        _tankParts.turret.transform.localRotation = Quaternion.Euler(0, _yawAngle * Mathf.Rad2Deg, 0);

        for (int i = 0; i < numberOfPoints; i++) {
            var t = i * _tEnd / numberOfPoints;

            var horizontalDist = launchSpeed * t * Mathf.Cos(_launchAngle);

            var xDist = Mathf.Sin(_yawAngle) * horizontalDist;
            var zDist = Mathf.Cos(_yawAngle) * horizontalDist;

            var yDist = -.5f * _gravityValue * t * t + launchSpeed * t * Mathf.Sin(_launchAngle);

            var posChange = new Vector3(xDist, yDist, zDist);

            points[i] = _tankParts.barrelTip.transform.position + posChange;
        }

        _lineRenderer.positionCount = numberOfPoints;
        _lineRenderer.SetPositions(points);
    }

    private float ImproveGuess(float guess) {
        return guess - ActualAngle(guess) / actualAngleDerivative(guess);
    }

    private float ActualAngle(float theta) {
        var t1 = -_gravityValue * _horizontalDistance * _horizontalDistance / launchSpeed / launchSpeed / 2 /
                 Mathf.Cos(theta) / Mathf.Cos(theta);

        var t2 = _gravityValue * barrelLength * _horizontalDistance / launchSpeed / launchSpeed / Mathf.Cos(theta);

        var t3 = _horizontalDistance * Mathf.Tan(theta);

        var t4 = -_yDistance - barrelLength * barrelLength * _gravityValue / 2 / launchSpeed / launchSpeed;

        return t1 + t2 + t3 + t4;
    }

    private float actualAngleDerivative(float theta) {
        var t1 = -_gravityValue * _horizontalDistance * _horizontalDistance / launchSpeed / launchSpeed / 2 /
                 Mathf.Cos(theta) / Mathf.Cos(theta) * Mathf.Tan(theta);
        
        var t2 = _gravityValue * barrelLength * _horizontalDistance / launchSpeed / launchSpeed / Mathf.Cos(theta) * Mathf.Tan(theta);

        var t3 = _horizontalDistance / Mathf.Cos(theta) / Mathf.Cos(theta);

        return t1 + t2 + t3;
    }
}