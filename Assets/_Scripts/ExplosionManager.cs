using Unity.Netcode;
using UnityEngine;

public class ExplosionManager : NetworkBehaviour {
    [SerializeField] private ParticleSystem particlesPrefab;
    [SerializeField] private AudioSource explosionAudioPrefab;

    public static ExplosionManager Manager;

    private void Awake() {
        Manager = this;
    }

    [ClientRpc]
    public void SpawnExplosionClientRpc(Vector3 pos, Vector3 normal, Vector3 velocity) {
        var rot = Quaternion.LookRotation(normal);

        var explosionParticles = Instantiate(particlesPrefab, pos, Quaternion.identity);

        var shape = explosionParticles.shape;
        shape.rotation = rot.eulerAngles;

        explosionParticles.GetComponent<Rigidbody>().velocity = velocity / 10f;

        explosionParticles.Play();

        var explosionAudio = Instantiate(explosionAudioPrefab, pos, Quaternion.identity);
        explosionAudio.Play();

        Destroy(explosionAudio.gameObject, explosionAudio.clip.length + .1f);
    }
}