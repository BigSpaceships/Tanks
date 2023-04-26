using UnityEngine;
using UnityEngine.InputSystem;

public class Aim : MonoBehaviour {
    [SerializeField] private GameObject aimObject;
    [SerializeField] private int numberOfPoints;

    private GameObject _focusedTank;
    private TankParts _tankParts;
    private LineRenderer _lineRenderer;

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

        _tEnd = _horizontalDistance / launchSpeed / Mathf.Cos(launchAngle);

        _pathLength = Util.Integrate((float x) => Mathf.Sqrt(Mathf.Pow(launchSpeed * Mathf.Cos(launchAngle), 2) +
                                                             Mathf.Pow(
                                                                 -_gravityValue * x +
                                                                 launchSpeed * Mathf.Sin(launchAngle), 2)), 0, _tEnd,
            100);

        var yawAngle = Mathf.Atan2(relativeVector.x, relativeVector.z);

        tankData.UpdateTargetAngles(new Vector2(launchAngle, yawAngle));
    }

    void DrawPath() {
        if (_focusedTank == null) return;

        var points = new Vector3[numberOfPoints];

        var angles = _focusedTank.GetComponent<TankData>().GetAngles();
        var launchAngle = angles.x;
        var yawAngle = angles.y;

        if (float.IsNaN(launchAngle)) {
            _lineRenderer.positionCount = numberOfPoints;
            _lineRenderer.SetPositions(points);

            return;
        }

        for (int i = 0; i < numberOfPoints; i++) {
            var t = i * _tEnd / numberOfPoints;

            var horizontalDist = launchSpeed * t * Mathf.Cos(launchAngle);

            var xDist = Mathf.Sin(yawAngle) * horizontalDist;
            var zDist = Mathf.Cos(yawAngle) * horizontalDist;

            var yDist = -.5f * _gravityValue * t * t + launchSpeed * t * Mathf.Sin(launchAngle);

            var posChange = new Vector3(xDist, yDist, zDist);

            points[i] = _tankParts.barrelTip.transform.position + posChange;
        }

        _lineRenderer.positionCount = numberOfPoints;
        _lineRenderer.SetPositions(points);
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
}