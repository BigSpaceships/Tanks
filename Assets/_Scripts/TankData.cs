using System;
using TMPro;
using Unity.Collections;
using Unity.Netcode;

public class TankData : NetworkBehaviour {
    private NetworkVariable<FixedString32Bytes> _name = new("");

    private void OnEnable() {
        UpdateNamePlate();
    }

    public override void OnNetworkSpawn() {
        _name.OnValueChanged += (_, _) => UpdateNamePlate();

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
}