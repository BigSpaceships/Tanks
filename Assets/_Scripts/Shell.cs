using UnityEngine;

public class Shell : MonoBehaviour {
    private Rigidbody _rb;

    [SerializeField] private ParticleSystem particles;

    private void Awake() {
        _rb = GetComponent<Rigidbody>();
    }

    private void Update() {
        if (_rb.velocity.sqrMagnitude > float.Epsilon) {
            transform.forward = _rb.velocity.normalized;
        }
    }

    private void OnCollisionEnter(Collision collision) {
        var explosionParticles = Instantiate(particles, transform.position, Quaternion.identity);

        Destroy(explosionParticles.gameObject, 2f);
        Destroy(gameObject);
    }
}