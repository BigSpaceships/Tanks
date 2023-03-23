using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class TankData : NetworkBehaviour {
    private NetworkVariable<FixedString32Bytes> _name = new("");
    private NetworkVariable<int> _health = new NetworkVariable<int>();

    public RespawnManager rs;

    private void OnEnable() {
        UpdateNamePlate();
        _health.Value = 100;
    }

    public override void OnNetworkSpawn() {
        Debug.Log(_name.Value);
        _health.Value = 100;

        _name.OnValueChanged += (_, _) => UpdateNamePlate();
        _health.OnValueChanged += (_, _) => UpdateNamePlate();

        if (IsOwner && IsClient) {
            ChangeName(PlayGUIManager.Manager.GetName());
        }
        else if (IsClient) {
            UpdateNamePlate();
        }
    }

    public override void OnNetworkDespawn() {
        _name.OnValueChanged -= (_, _) => UpdateNamePlate();
        _health.OnValueChanged -= (_, _) => UpdateNamePlate();
    }

    private void UpdateNamePlate() {
        var nameText = Array.Find(gameObject.GetComponentsInChildren<TextMeshProUGUI>(),
            text => text.name == "Name Text");

        nameText.text = _name.Value.ToString() + " - " + _health.Value.ToString();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChangeNameServerRpc(string newName) {
        _name.Value = newName;
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChangeHealthServerRpc(int newHealth)
    {
        _health.Value -= newHealth;
        ChangeHealthClientRpc();
    }

    [ClientRpc]
    public void ChangeHealthClientRpc()
    {
        if (_health.Value == 0)
        {
            StartCoroutine(RespawnTank(1));
        }
    }
    
    public void ChangeName(string newName) {
        if (IsClient) {
            ChangeNameServerRpc(newName);
        }
    }

    public void ChangeHealth(int h)
    {
        print(_health.Value);
        // ChangeName($"{_name.Value} - {_health.Value}");
        ChangeHealthServerRpc(h);
    }

    IEnumerator RespawnTank(float time)
    {
        Transform t = gameObject.transform.Find("Tank Idle");
        t.gameObject.SetActive(false);
        gameObject.GetComponent<BoxCollider>().enabled = false;
        yield return new WaitForSeconds(time);
        transform.position = new Vector3(0, 10, 0);
        _health.Value = 100;
        t.gameObject.SetActive(true);
        gameObject.GetComponent<BoxCollider>().enabled = true;
    }


}