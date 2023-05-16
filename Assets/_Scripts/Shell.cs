using Unity.Netcode;
using UnityEngine;

public class Shell : NetworkBehaviour {
    private Rigidbody _rb;

    private void Awake() {
        _rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn() {
        if (!IsServer) {
            _rb.isKinematic = true;
        }
    }

    private void Update() {
        if (!IsClient) {
            if (_rb.velocity.sqrMagnitude > float.Epsilon) {
                transform.forward = _rb.velocity.normalized;
            }
        }

        if (IsServer) {
            TransformClientRpc(transform.position, transform.rotation.eulerAngles);
        }
    }

    private void OnCollisionEnter(Collision collision) {
        if (IsServer) {
            ExplosionManager.Manager.SpawnExplosionClientRpc(transform.position, collision.GetContact(0).normal,
                _rb.velocity);
            Destroy(gameObject);
        }
    }

    [ClientRpc]
    private void TransformClientRpc(Vector3 pos, Vector3 rot) {
        transform.position = pos;
        transform.rotation = Quaternion.Euler(rot);
    }
}