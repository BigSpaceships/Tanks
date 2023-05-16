using UnityEngine;

public class Shell : MonoBehaviour {
    private Rigidbody _rb;

    private void Awake() {
        _rb = GetComponent<Rigidbody>();
    }

    private void Update() {
        if (_rb.velocity.sqrMagnitude > float.Epsilon) {
            transform.forward = _rb.velocity.normalized;
        }
    }

    private void OnCollisionEnter(Collision collision) {
        ExplosionManager.Manager.SpawnExplosionClientRpc(transform.position, collision.GetContact(0).normal, _rb.velocity);
        Destroy(gameObject);
    }
}