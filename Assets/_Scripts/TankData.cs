using System;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class TankData : NetworkBehaviour {
    private readonly NetworkVariable<FixedString32Bytes> _name = new("");
    private readonly NetworkVariable<Vector3> _targetPosition = new();

    private void OnEnable() {
        UpdateNamePlate();
    }

    public override void OnNetworkSpawn() {
        _name.OnValueChanged += (_, _) => UpdateNamePlate();
        // _targetPosition.OnValueChanged += 

        if (IsOwner && IsClient) {
            ChangeName(PlayGUIManager.Manager.GetName());
        }
        else if (IsClient) {
            UpdateNamePlate();
        }
    }

    public override void OnNetworkDespawn() {
        _name.OnValueChanged -= (_, _) => UpdateNamePlate();
    }

    private void UpdateNamePlate() {
        var nameText = Array.Find(gameObject.GetComponentsInChildren<TextMeshProUGUI>(),
            text => text.name == "Name Text");

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

    private void OnDrawGizmos() {
        Debug.DrawLine(_targetPosition.Value, _targetPosition.Value + Vector3.up, Color.green);
    }
}