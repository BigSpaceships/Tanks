using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class TankShooting : NetworkBehaviour {
    public InputActionMap controls;

    public float launchSpeed;

    [SerializeField] private GameObject shotPrefab;

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

                controls["Shoot"].performed += OnShoot;
            }
        }
    }

    private void OnShoot(InputAction.CallbackContext context) {
        if (!NetworkManager.Singleton.IsClient) return;

        if (!NetworkObject.IsOwner) return;

        ShootServerRpc();
    }

    [ServerRpc]
    private void ShootServerRpc() {
        var launchObject = GetComponent<TankParts>().barrelTip.transform;
        var newShot = Instantiate(shotPrefab, launchObject.position, launchObject.rotation);

        newShot.GetComponent<NetworkObject>().Spawn();

        newShot.GetComponent<Rigidbody>().AddForce(launchObject.transform.forward * launchSpeed, ForceMode.Impulse);
    }
}