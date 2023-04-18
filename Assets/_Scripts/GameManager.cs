using Unity.Netcode;
using UnityEngine;

public class GameManager : MonoBehaviour {
    public static GameObject GetFocusedTank() {
        if (NetworkManager.Singleton.IsClient && (NetworkManager.Singleton.LocalClient != null)) {
            var focusedTank = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;

            if (!focusedTank) {
                // focusedTank = GameObject.FindGameObjectsWithTag("Player")[0];
            }

            return focusedTank;
        }

        return null;
    }
}