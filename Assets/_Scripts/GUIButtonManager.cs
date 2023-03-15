using System.Net;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GUIButtonManager : MonoBehaviour {
    public string ip;
    public string clientIp;

    private void OnGUI() {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer) {
            StartButtons();
        }
        else {
            StatusLabels();
        }

        GUILayout.EndArea();
    }

    private void StartButtons() {
        if (GUILayout.Button("Host")) StartHost();
        if (GUILayout.Button("Client")) StartClient();
        if (GUILayout.Button("Server")) NetworkManager.Singleton.StartServer();
        GUILayout.Label("Client IP: ", "label");
        ip = GUILayout.TextField(ip, 24, "textfield");
    }

    private void StatusLabels() {
        var mode = NetworkManager.Singleton.IsHost ? "Host" : NetworkManager.Singleton.IsClient ? "Client" : "Server";

        GUILayout.Label("Transport: " + NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode);
        var hostName = Dns.GetHostName();
        GUILayout.Label("IP: " + Dns.GetHostEntry(hostName).AddressList[0]);
        
        if (GUILayout.Button("Disconnect")) Disconnect();
    }

    private void StartHost() {
        // string hostName = Dns.GetHostName();
        // clientIp = Dns.GetHostEntry(hostName).AddressList[0].ToString();
        // NetworkManager nm = GetComponentInParent<NetworkManager>();
        NetworkManager.Singleton.StartHost();
        // NetworkManager.Singleton.StartClient();
    }

    private void StartClient() {
        NetworkManager nm = GetComponentInParent<NetworkManager>();
        NetworkManager.Singleton.StartClient();
    }

    private void Disconnect() {
        
        NetworkManager.Singleton.Shutdown();

        SceneManager.LoadScene(0);
    }
}