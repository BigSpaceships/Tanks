using Unity.Netcode;
using UnityEngine;

public class ExplosionManager : NetworkBehaviour {
    [SerializeField] private ParticleSystem particles;

    public static ExplosionManager Manager;

    private void Awake() {
        Manager = this;
    }

    [ClientRpc]
    public void SpawnExplosionClientRpc(Vector3 pos) {
        var explosionParticles = Instantiate(particles, pos, Quaternion.identity);

        Destroy(explosionParticles.gameObject, 2f);
    }
}