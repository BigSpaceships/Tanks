using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class TankShooting : NetworkBehaviour {
    public InputActionMap controls;
    public GameObject shellPrefab;
    public Transform shotSpawn;
    private GameObject currentShell;

    [Range(10, 100)] public float shotForce;
    [SerializeField] public float barrelMoveSpeed = 5f;

    private void OnEnable() {
        UpdateControls();
    }

    public override void OnNetworkSpawn() {
        UpdateControls();
    }

    private void UpdateControls() {
        if (NetworkManager.Singleton.IsClient) {
            if (NetworkObject.IsOwner) {
                controls.Enable();

                controls["Fire"].performed += OnFire;
            }
        }
    }

    private void OnFire(InputAction.CallbackContext context) {
        if (!NetworkManager.Singleton.IsClient) return;

        if (!NetworkObject.IsOwner) return;

        FireServerRPC(shotSpawn.position, shotSpawn.rotation);
    }

    [ServerRpc]
    private void FireServerRPC(Vector3 v, Quaternion rot) {
        GameObject shell = Instantiate(shellPrefab, v, rot);
        shell.GetComponent<NetworkObject>().Spawn();
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        shell.GetComponent<Rigidbody>().AddForce(forward * shotForce, ForceMode.Impulse);

        Destroy(shell, 5);
    }
}