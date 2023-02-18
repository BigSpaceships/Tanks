using System;
using System.Collections;
using System.Collections.Generic;
using SocketIOClient;
using Unity.WebRTC;
using UnityEngine;

[ExecuteAlways]
public class ConnectionTesting : MonoBehaviour {
    public SocketIOUnity socket;
    
    private RTCPeerConnection localPC;

    private DelegateOnIceCandidate localOnIceCandidate;
    // private DelegateOnIceCandidate remoteOnIceCandidate;
    
    private DelegateOnIceConnectionChange localOnIceConnectionChange;
    // private DelegateOnIceConnectionChange remoteOnIceConnectionChange;
    
    public void Setup() {
        localOnIceCandidate = candidate => { OnIceCandidate(localPC, candidate); };
        // remoteOnIceCandidate = candidate => { OnIceCandidate(remotePC, candidate); };

        localOnIceConnectionChange = state => { OnIceConnectionChange(localPC, state); };
        // remoteOnIceConnectionChange = state => { OnIceConnectionChange(remotePC, state); };
    }
    
    public void ConnectSignalingServer() {
        DisconnectSignalingServer();
        
        var uri = new Uri("https://TanksSignalingServer.bigspaceships.repl.co");
        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        });
        
        socket.Connect();

        socket.OnConnected += (sender , args) => {
            var senderSocket = (SocketIOUnity) sender;
            socket.Emit("type", "unity");
        };

        socket.On("test", response => {
            Debug.Log(response.GetValue<string>());
        });
    }

    // public IEnumerator StartConnection() {
          
    // }

    public void SendMessage() {
        socket.Emit("test", "HELOOOO");
    }

    public void DisconnectSignalingServer() {
        if (socket is not {Connected: true}) return;
        
        Debug.Log("disconnecting");
        socket.Disconnect();
        socket.Dispose();
    }

    private void OnDisable() {
        DisconnectSignalingServer();
    }

    private RTCConfiguration GetConfig() {
        RTCConfiguration config = default;
        
        config.iceServers = new RTCIceServer[]
        {
            new RTCIceServer { urls = new string[] { "stun:stun.l.google.com:19302" } }
        };

        return config;
    }

    private void OnIceCandidate(RTCPeerConnection pc, RTCIceCandidate candidate) {
        localPC.AddIceCandidate(candidate);
        Debug.Log($"{GetName(pc) } ICE Candidate: {candidate.Candidate}");
    } 
    string GetName(RTCPeerConnection pc)
    {
        return (pc == localPC) ? "local" : "remote";
    }
    
    void OnIceConnectionChange(RTCPeerConnection pc, RTCIceConnectionState state) // just log ig
    {
        switch (state)
        {
            case RTCIceConnectionState.New:
                Debug.Log($"{GetName(pc)} IceConnectionState: New");
                break;
            case RTCIceConnectionState.Checking:
                Debug.Log($"{GetName(pc)} IceConnectionState: Checking");
                break;
            case RTCIceConnectionState.Closed:
                Debug.Log($"{GetName(pc)} IceConnectionState: Closed");
                break;
            case RTCIceConnectionState.Completed:
                Debug.Log($"{GetName(pc)} IceConnectionState: Completed");
                break;
            case RTCIceConnectionState.Connected:
                Debug.Log($"{GetName(pc)} IceConnectionState: Connected");
                break;
            case RTCIceConnectionState.Disconnected:
                Debug.Log($"{GetName(pc)} IceConnectionState: Disconnected");
                break;
            case RTCIceConnectionState.Failed:
                Debug.Log($"{GetName(pc)} IceConnectionState: Failed");
                break;
            case RTCIceConnectionState.Max:
                Debug.Log($"{GetName(pc)} IceConnectionState: Max");
                break;
            default:
                break;
        }
    }
}
