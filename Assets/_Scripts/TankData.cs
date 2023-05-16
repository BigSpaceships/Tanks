using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TankData : NetworkBehaviour {
    private readonly NetworkVariable<FixedString32Bytes> _name = new("");
    private readonly NetworkVariable<Vector3> _targetPosition = new(Vector3.zero, NetworkVariableReadPermission.Owner);

    private readonly NetworkVariable<float> _health = new();

    // x is pitch y is yaw
    private readonly NetworkVariable<Vector2> _targetLaunchAngles = new();

    private TankParts _parts;

    public float maxHealth = 100;

    private void Awake() {
        _parts = GetComponent<TankParts>();
    }

    private void OnEnable() {
        UpdateNamePlate();
    }

    private void Update() {
        var angles = GetAngles();
        var launchAngle = angles.x;
        var yawAngle = angles.y;

        var targetRot = Quaternion.Euler(-launchAngle * Mathf.Rad2Deg, yawAngle * Mathf.Rad2Deg, 0);

        var targetDir = targetRot * Vector3.forward;
        targetDir = transform.InverseTransformDirection(targetDir);

        var rotChange = Quaternion.FromToRotation(Vector3.forward, targetDir);

        // TODO: slow turn
        _parts.barrel.transform.localRotation = Quaternion.Euler(rotChange.eulerAngles.x, 0, 0);
        _parts.turret.transform.localRotation = Quaternion.Euler(0, rotChange.eulerAngles.y, 0);
    }

    public override void OnNetworkSpawn() {
        _name.OnValueChanged += (_, _) => UpdateNamePlate();
        _health.OnValueChanged += (_, _) => UpdateHealth();

        if (IsOwner && IsClient) {
            ChangeName(PlayGUIManager.Manager.GetName());
        }

        if (IsServer) {
            _health.Value = maxHealth;
            _parts.healthSlider.GetComponent<Slider>().maxValue = maxHealth;
        }

        if (IsClient) {
            UpdateNamePlate();
            UpdateHealth();
        }
    }

    public override void OnNetworkDespawn() {
        _name.OnValueChanged -= (_, _) => UpdateNamePlate();
        _health.OnValueChanged -= (_, _) => UpdateHealth();
    }

    private void UpdateNamePlate() {
        var nameText = _parts.namePlate.GetComponent<TextMeshProUGUI>();

        nameText.text = _name.Value.ToString();
    }

    [ServerRpc]
    private void ChangeNameServerRpc(string newName) {
        _name.Value = newName;
    }

    public void ChangeName(string newName) {
        if (IsClient && IsOwner) {
            ChangeNameServerRpc(newName);
        }
    }

    [ServerRpc]
    private void UpdateTargetPositionServerRpc(Vector3 pos) {
        _targetPosition.Value = pos;
    }

    public void UpdateTargetPosition(Vector3 pos) {
        if (IsClient && IsOwner) {
            UpdateTargetPositionServerRpc(pos);
        }
    }

    public Vector3 GetTargetPosition() {
        if (IsServer || IsOwner) return _targetPosition.Value;

        Debug.LogError("Cannot access other tanks targeted positions");
        return Vector3.zero;
    }

    [ServerRpc]
    private void UpdateTargetAnglesServerRpc(Vector2 angles) {
        _targetLaunchAngles.Value = angles;
    }

    public void UpdateTargetAngles(Vector2 angles) {
        if (IsClient && IsOwner) {
            UpdateTargetAnglesServerRpc(angles);
        }
    }

    public Vector2 GetAngles() {
        return _targetLaunchAngles.Value;
    }

    private void UpdateHealth() {
        _parts.healthSlider.GetComponent<Slider>().value = _health.Value;
    }

    public void DealDamage(float amount) {
        if (IsServer) {
            var newHealth = _health.Value - amount;

            _health.Value = Mathf.Clamp(newHealth, 0, maxHealth);
        }
    }
}