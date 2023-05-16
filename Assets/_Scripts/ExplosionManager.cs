using Unity.Netcode;
using UnityEngine;

public class ExplosionManager : NetworkBehaviour {
    [SerializeField] private ParticleSystem particles;

    public static ExplosionManager Manager;

    private void Awake() {
        Manager = this;
    }

    [ClientRpc]
    public void SpawnExplosionClientRpc(Vector3 pos, Vector3 normal, Vector3 velocity) {
        var rot = Quaternion.LookRotation(normal);
        
        var explosionParticles = Instantiate(particles, pos, Quaternion.identity);

        var shape = explosionParticles.shape;
        shape.rotation = rot.eulerAngles;
        
        // var main = explosionParticles.main;
        // main.emitterVelocity = velocity;

        explosionParticles.GetComponent<Rigidbody>().velocity = velocity / 10f;
        
        explosionParticles.Play();
        
        Debug.DrawRay(pos, velocity, Color.green, 1);
    }
}