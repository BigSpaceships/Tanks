using Unity.Netcode;
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

    private Camera _camera;

    private float _zoomInput;
    private Vector2 _orbitInput;

    public bool[] display;

    private void Awake() {
        _zoomInput = 0;

        _camera = transform.GetChild(0).GetComponent<Camera>();
    }

    private void OnEnable() {
        cameraControls.Enable();

        cameraControls["Zoom"].performed += OnZoom;
        cameraControls["Zoom"].canceled += OnZoom;

        cameraControls["Orbit"].performed += OnOrbit;
        cameraControls["Orbit"].canceled += OnOrbit;
    }

    private void Update() {
        tank = GetFocusedTank();

        if (!tank) {
            return;
        }

        distanceFromTank -= _zoomInput * zoomSpeed;

        pitch -= _orbitInput.y * sensitivity;
        yaw += _orbitInput.x * sensitivity;

        pitch = Mathf.Clamp(pitch, -20, 85);

        transform.rotation = Quaternion.Euler(pitch, 0, 0);

        transform.Rotate(Vector3.up, yaw, Space.World);

        transform.position = tank.transform.Find("Camera Follow").position;

        var layerMask = LayerMask.GetMask("Default");

        display = MaskToBools(layerMask);

        var maxDistance = Physics.SphereCast(transform.position, .5f, -transform.forward, out RaycastHit hit,
            distanceRange.y, layerMask)
            ? hit.distance
            : distanceRange.y;

        var actualDistanceFromTank = Mathf.Clamp(distanceFromTank, distanceRange.x, maxDistance);

        _camera.transform.localPosition = new Vector3(0, 0, -actualDistanceFromTank);
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
        _orbitInput = context.canceled ? Vector2.zero : context.ReadValue<Vector2>();
    }

    private bool[] MaskToBools(int layerMask) {
        bool[] hasLayers = new bool[32];

        for (int i = 0; i < 32; i++) {
            if (layerMask == (layerMask | (1 << i))) {
                hasLayers[i] = true;
            }
        }

        return hasLayers;
    }

    private GameObject GetFocusedTank() {
        if (NetworkManager.Singleton.IsClient && (NetworkManager.Singleton.LocalClient != null)) {
            var focusedTank = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;

            if (!focusedTank) {
                // focusedTank = GameObject.FindGameObjectsWithTag("Player")[0];
            }

            return focusedTank;
        }

        return null;
    }
}