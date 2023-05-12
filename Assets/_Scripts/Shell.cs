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
        // Debug.Log("Boom");
        Destroy(gameObject);
    }
}