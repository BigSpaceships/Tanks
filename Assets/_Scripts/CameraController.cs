using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour {
    public InputActionMap cameraControls;

    public GameObject tank;

    [SerializeField] private float zoomSpeed = 1;
    [SerializeField] private Vector2 distanceRange;

    [SerializeField] private float distanceFromTank;
    [SerializeField] private float pitch;
    [SerializeField] private float yaw;

    private float _zoomInput;

    private void Awake() {
        _zoomInput = 0;
    }

    private void OnEnable() {
        cameraControls.Enable();

        cameraControls["Zoom"].performed += OnZoom;
        cameraControls["Zoom"].canceled += OnZoom;
    }

    private void Update() {
        distanceFromTank -= _zoomInput * zoomSpeed;
        distanceFromTank = Mathf.Clamp(distanceFromTank, distanceRange.x, distanceRange.y);

        gameObject.transform.position = tank.transform.position + Vector3.back * distanceFromTank;

        gameObject.transform.LookAt(tank.transform);
    }

    public void OnZoom(InputAction.CallbackContext context) {
        if (context.canceled) {
            _zoomInput = 0;
        }
        else {
            _zoomInput = Mathf.Sign(context.ReadValue<float>());
        }
    }
}