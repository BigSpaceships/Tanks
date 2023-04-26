using UnityEngine;

public class TankParts : MonoBehaviour {
    public GameObject namePlate;
    public GameObject turret;
    public GameObject barrel;
    public GameObject barrelTip;

    public float BarrelLength => barrel.transform.InverseTransformPoint(barrelTip.transform.position).z;
}