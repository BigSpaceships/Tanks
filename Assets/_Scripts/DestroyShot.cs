using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DestroyShot : NetworkBehaviour
{
    private void OnEnable()
    {
        StartDespawn();
    }

    public override void OnNetworkSpawn()
    {
        StartDespawn();
    }

    private void StartDespawn()
    {
        if (NetworkManager.Singleton.IsClient)
        {
            if (NetworkObject.IsOwner)
            {
                NetDespawnServerRPC(5);
            }
        }
    }

    [ServerRpc]
    private void NetDespawnServerRPC(int t)
    {
        Destroy(gameObject, t);
    }
}
