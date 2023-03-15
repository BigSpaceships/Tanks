using System;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class TankData : NetworkBehaviour {
    private NetworkVariable<FixedString32Bytes> _name = new("");
    private NetworkVariable<int> _health = new NetworkVariable<int>();

    private void OnEnable() {
        UpdateNamePlate("", "");
        _health.Value = 100;
    }

    public override void OnNetworkSpawn() {
        Debug.Log(_name.Value);

        _name.OnValueChanged += UpdateNamePlate;

        if (IsOwner && IsClient) {
            ChangeName(PlayGUIManager.Manager.GetName());
        }
        else if (IsClient) {
            UpdateNamePlate("", "");
        }
    }

    public override void OnNetworkDespawn() {
        _name.OnValueChanged -= UpdateNamePlate;
    }

    private void UpdateNamePlate(FixedString32Bytes previous, FixedString32Bytes current) {
        var nameText = Array.Find(gameObject.GetComponentsInChildren<TextMeshProUGUI>(),
            text => text.name == "Name Text");

        nameText.text = _name.Value.ToString();
    }

    [ServerRpc]
    private void ChangeNameServerRpc(string newName) {
        _name.Value = newName;
    }

    [ServerRpc]
    private void ChangeHealthServerRpc(int newHealth)
    {
        _health.Value -= newHealth;
    }
    
    public void ChangeName(string newName) {
        if (IsClient && IsOwner) {
            ChangeNameServerRpc(newName);
        }
    }

    public void ChangeHealth(int h)
    {
        print(_health.Value);
        if (_health.Value <= 0)
        {
            DestroyTank();
        }
        ChangeHealthServerRpc(h);
    }

    private void DestroyTank() // TODO: add more to make the game better
    {
        Destroy(gameObject);
    }
}