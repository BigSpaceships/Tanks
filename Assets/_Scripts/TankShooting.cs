using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
     

public class TankShooting : NetworkBehaviour
{
    public InputActionMap controls;
    public GameObject shell;
    public Transform shotSpawn;
    private GameObject currentShell;

    [Range(10, 100)] public float shotForce;

    private void OnEnable()
    {
        UpdateControls();
    }

    public override void OnNetworkSpawn()
    {
        UpdateControls();
    }

    private void UpdateControls()
    {
        if (NetworkManager.Singleton.IsClient)
        {
            if (NetworkObject.IsOwner)
            {
                controls.Enable();

                controls["Fire"].performed += OnFire;
            }
        }
    }

    private void OnFire(InputAction.CallbackContext context)
    {
        if (!NetworkManager.Singleton.IsClient) return;
        
        if (!NetworkObject.IsOwner) return;
        
        FireServerRPC(shotSpawn.position, shotSpawn.rotation);
    }

    [ServerRpc]
    private void FireServerRPC(Vector3 v, Quaternion rot)
    {
        GameObject c_Shell = Instantiate(shell, v, rot);
        c_Shell.GetComponent<NetworkObject>().Spawn();
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        c_Shell.GetComponent<Rigidbody>().AddForce(forward * shotForce, ForceMode.Impulse);
    }
}
