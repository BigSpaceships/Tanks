using UnityEngine;
using UnityEngine.InputSystem;

public class Aim : MonoBehaviour {
    [SerializeField] private GameObject aimObject;
    [SerializeField] private int numberOfPoints;
    [SerializeField] private float tCheckIncrement;

    private GameObject _focusedTank;
    private TankParts _tankParts;
    private LineRenderer _lineRenderer;

    private float _pathLength;
    private float _tEnd;
    private float _horizontalDistance;
    private float _yDistance;

    private Vector3 _hitPoint;
    private Vector3 _hitNormal;

    private float _gravityValue;

    [SerializeField] private float launchSpeed;
    public float barrelLength;

    private void Start() {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    private void Update() {
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

        UpdateTargetDisplay();
    }

    private void CalculatePathValues() {
        if (_focusedTank == null) return;

        _gravityValue = -Physics.gravity.y;

        var tankData = _focusedTank.GetComponent<TankData>();

        barrelLength = _tankParts.BarrelLength;

        var relativeVector =
            tankData.GetTargetPosition() - _tankParts.barrel.transform.position;

        _horizontalDistance = new Vector3(relativeVector.x, 0, relativeVector.z).magnitude;
        _yDistance = relativeVector.y;

        var launchAngle = Mathf.Atan(
            (launchSpeed * launchSpeed - Mathf.Sqrt(Mathf.Pow(launchSpeed, 4) - _gravityValue *
                (_gravityValue * _horizontalDistance * _horizontalDistance +
                 2 * relativeVector.y * launchSpeed * launchSpeed))) / (_gravityValue * _horizontalDistance));

        launchAngle = ImproveGuess(launchAngle);
        launchAngle = ImproveGuess(launchAngle);
        launchAngle = ImproveGuess(launchAngle);

        _tEnd = GetEndTime();

        _pathLength = Util.Integrate((float x) => Mathf.Sqrt(Mathf.Pow(launchSpeed * Mathf.Cos(launchAngle), 2) +
                                                             Mathf.Pow(
                                                                 -_gravityValue * x +
                                                                 launchSpeed * Mathf.Sin(launchAngle), 2)), 0, _tEnd,
            100);

        var yawAngle = Mathf.Atan2(relativeVector.x, relativeVector.z);

        if (float.IsNaN(launchAngle))
            launchAngle = Mathf.PI / 4;

        tankData.UpdateTargetAngles(new Vector2(launchAngle, yawAngle));
    }

    private void DrawPath() {
        if (_focusedTank == null) return;

        var points = new Vector3[numberOfPoints];

        var angles = _focusedTank.GetComponent<TankData>().GetAngles();
        var launchAngle = angles.x;
        var yawAngle = angles.y;

        if (float.IsNaN(launchAngle) || float.IsNaN(_tEnd)) {
            _lineRenderer.positionCount = numberOfPoints;
            _lineRenderer.SetPositions(points);

            return;
        }

        for (var i = 0; i < numberOfPoints; i++) {
            var t = i * _tEnd / numberOfPoints;

            points[i] = GetPointAtTime(t);
        }

        _lineRenderer.positionCount = numberOfPoints;
        _lineRenderer.SetPositions(points);
    }

    private Vector3 GetPointAtTime(float t) {
        var angles = _tankParts.tankData.GetAngles();

        var launchAngle = angles.x;
        var yawAngle = angles.y;

        var horizontalDist = launchSpeed * t * Mathf.Cos(launchAngle);

        var xDist = Mathf.Sin(yawAngle) * horizontalDist;
        var zDist = Mathf.Cos(yawAngle) * horizontalDist;

        var yDist = -.5f * _gravityValue * t * t + launchSpeed * t * Mathf.Sin(launchAngle);

        var posChange = new Vector3(xDist, yDist, zDist);

        return _tankParts.barrelTip.transform.position + posChange;
    }

    private float ImproveGuess(float guess) {
        return guess - ActualAngle(guess) / ActualAngleDerivative(guess);
    }

    private float ActualAngle(float theta) {
        var t1 = -_gravityValue * _horizontalDistance * _horizontalDistance / launchSpeed / launchSpeed / 2 /
                 Mathf.Cos(theta) / Mathf.Cos(theta);

        var t2 = _gravityValue * barrelLength * _horizontalDistance / launchSpeed / launchSpeed / Mathf.Cos(theta);

        var t3 = _horizontalDistance * Mathf.Tan(theta);

        var t4 = -_yDistance - barrelLength * barrelLength * _gravityValue / 2 / launchSpeed / launchSpeed;

        return t1 + t2 + t3 + t4;
    }

    private float ActualAngleDerivative(float theta) {
        var t1 = -_gravityValue * _horizontalDistance * _horizontalDistance / launchSpeed / launchSpeed / 2 /
            Mathf.Cos(theta) / Mathf.Cos(theta) * Mathf.Tan(theta);

        var t2 = _gravityValue * barrelLength * _horizontalDistance / launchSpeed / launchSpeed / Mathf.Cos(theta) *
                 Mathf.Tan(theta);

        var t3 = _horizontalDistance / Mathf.Cos(theta) / Mathf.Cos(theta);

        return t1 + t2 + t3;
    }

    private float GetEndTime() {
        for (int i = 0; i < 1000; i++) {
            var startT = i * tCheckIncrement;
            var endT = (i + 1) * tCheckIncrement;

            var intersectTime = CheckLineSegment(startT, endT);

            if (float.IsNaN(intersectTime)) {
                continue;
            }

            return intersectTime;
        }

        return float.NaN;
    }

    private float CheckLineSegment(float tStart, float tEnd) {
        var startPos = GetPointAtTime(tStart);
        var endPos = GetPointAtTime(tEnd);

        if (Physics.Linecast(startPos, endPos, out var hit)) {
            _hitPoint = hit.point;
            _hitNormal = hit.normal;

            var relativePos = hit.point - _tankParts.barrelTip.transform.position;

            var xDist = new Vector2(relativePos.x, relativePos.z).magnitude;

            var launchAngle = _tankParts.tankData.GetAngles().x;

            return (xDist - barrelLength * Mathf.Cos(launchAngle)) / launchSpeed / Mathf.Cos(launchAngle);
        }

        return float.NaN;
    }

    private void UpdateTargetDisplay() {
        aimObject.transform.position = _hitPoint;
        aimObject.transform.LookAt(aimObject.transform.position + _hitNormal);

        var relativeVector = Camera.main.transform.position - aimObject.transform.position;

        var scaleFactor = relativeVector.magnitude / 15;

        aimObject.transform.GetChild(0).transform.localScale = Vector3.one * scaleFactor;
        Debug.Log(scaleFactor);
    }
}