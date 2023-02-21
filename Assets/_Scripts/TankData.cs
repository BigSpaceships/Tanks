using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class TankData : NetworkBehaviour {    
    private NetworkVariable<FixedString32Bytes> _name = new("");

    private void OnEnable() {
        UpdateNamePlate("", "");
    }

    public override void OnNetworkSpawn() {
        _name.OnValueChanged += UpdateNamePlate;

        if (IsOwner && IsClient) {
            _name.Value = PlayGUIManager.Manager.GetName();
        }
    }

    public override void OnNetworkDespawn() {
        _name.OnValueChanged -= UpdateNamePlate;
    }

    private void UpdateNamePlate(FixedString32Bytes previous, FixedString32Bytes current) {
        var nameText = Array.Find(gameObject.GetComponentsInChildren<TextMeshProUGUI>(), text => text.name == "Name Text");

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
}
