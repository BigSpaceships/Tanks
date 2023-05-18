using UnityEngine;

public class TankParts : MonoBehaviour {
    public GameObject namePlate;
    public GameObject healthSlider;
    public GameObject turret;
    public GameObject barrel;
    public GameObject barrelTip;

    public AudioSource shotAudioSource;

    public TankData tankData;

    public float BarrelLength => barrel.transform.InverseTransformPoint(barrelTip.transform.position).z;
}