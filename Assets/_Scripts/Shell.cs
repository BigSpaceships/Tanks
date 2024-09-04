using Unity.Netcode;
using UnityEngine;

public class Shell : NetworkBehaviour {
    private Rigidbody _rb;

    [SerializeField] private float maxDamage;
    [SerializeField] private float damageRadius;
    [SerializeField] private AnimationCurve damageFalloff;

    private void Awake() {
        _rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn() {
        if (!IsServer) {
            _rb.isKinematic = true;
        }
    }

    private void Update() {
        if (IsServer) {
            if (_rb.velocity.sqrMagnitude > float.Epsilon) {
                transform.forward = _rb.velocity.normalized;
            }
            
            TransformClientRpc(transform.position, transform.rotation.eulerAngles);
        }
    }

    private void OnCollisionEnter(Collision collision) {
        if (IsServer) {
            ExplosionManager.Manager.SpawnExplosionClientRpc(transform.position, collision.GetContact(0).normal,
                _rb.velocity);
            Destroy(gameObject);

            var hitColliders =
                Physics.OverlapSphere(transform.position, damageRadius, LayerMask.GetMask("PlayerCollision"));

            foreach (var tankCollider in hitColliders) {
                var tank = tankCollider.GetComponentInParent<TankData>();

                var dist = (tank.transform.position - transform.position).magnitude;

                var damage = maxDamage * damageFalloff.Evaluate(dist / damageRadius);

                tank.DealDamage(damage);
            }
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TransformClientRpc(Vector3 pos, Vector3 rot) {
        transform.position = pos;
        transform.rotation = Quaternion.Euler(rot);
    }
}