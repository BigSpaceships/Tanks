using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour {
    public InputActionMap cameraControls;

    public GameObject tank;

    [SerializeField] private float zoomSpeed = 1;
    [SerializeField] private Vector2 distanceRange;

    [SerializeField] private float sensitivity;

    [SerializeField] private float distanceFromTank;
    [SerializeField] private float pitch;
    [SerializeField] private float yaw;

    private float _zoomInput;
    private Vector2 _orbitInput;

    private void Awake() {
        _zoomInput = 0;
    }

    private void OnEnable() {
        cameraControls.Enable();

        cameraControls["Zoom"].performed += OnZoom;
        cameraControls["Zoom"].canceled += OnZoom;

        cameraControls["Orbit"].performed += OnOrbit;
        cameraControls["Orbit"].canceled += OnOrbit;
    }

    private void Update() {
        distanceFromTank -= _zoomInput * zoomSpeed;
        distanceFromTank = Mathf.Clamp(distanceFromTank, distanceRange.x, distanceRange.y);

        pitch += _orbitInput.y * sensitivity;
        yaw += _orbitInput.x * sensitivity;

        pitch = Mathf.Clamp(pitch, 0, 85);

        transform.GetChild(0).localPosition = new Vector3(0, 0, -distanceFromTank);

        transform.rotation = Quaternion.Euler(pitch, 0, 0);

        transform.Rotate(Vector3.up, yaw, Space.World);

        transform.position = tank.transform.position;
    }

    private void OnZoom(InputAction.CallbackContext context) {
        if (context.canceled) {
            _zoomInput = 0;
        }
        else {
            _zoomInput = Mathf.Sign(context.ReadValue<float>());
        }
    }

    private void OnOrbit(InputAction.CallbackContext context) {
        if (context.canceled) {
            _orbitInput = Vector2.zero;
        }
        else {
            _orbitInput = context.ReadValue<Vector2>();
        }
    }
}