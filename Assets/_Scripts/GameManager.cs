using System;
using Unity.Netcode;
using UnityEngine;

public class GameManager : MonoBehaviour {

    public static GameManager Manager;
    
    private int _namelessPlayerCount;
    public string NextNamelessPlayer => "Player " + ++_namelessPlayerCount;
    
    private void Awake() {
        Manager = this;
        
        _namelessPlayerCount = 0;
    }

    public static GameObject GetFocusedTank() {
        if (NetworkManager.Singleton.IsClient && NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject) {
            var focusedTank = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;

            return focusedTank;
        }

        return null;
    }

    private void OnDisable() {
        Close();
    }

    private void OnDestroy() {
        Close();
    }

    private void Close() {
        Debug.Log("shutdown");
        NetworkManager.Singleton?.Shutdown();
    }
}