using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using SocketIOClient;
using Unity.Netcode;
using Unity.WebRTC;
using UnityEngine;

public class WebRtcTransport : NetworkTransport {
    private SocketIOUnity _socket;

    [SerializeField] private bool logNetworkDebug;

    private enum Type {
        Server,
        Client,
    }

    private ulong _lastId = 0;
    private ulong NextId => _lastId++;

    private Type _type;

    private Dictionary<ulong, WebRtcConnection> _peers = new();
    private Dictionary<string, ulong> _peerSocketIds = new();

    private void StartSocket() {
        var uri = new Uri("https://8080-bigspaceshi-tanksignals-itvkcauzscy.ws-us93.gitpod.io/");
        _socket = new SocketIOUnity(uri, new SocketIOOptions {
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        });

        _socket.Connect();

        _socket.OnConnected += (sender, args) => {
            Log("Socket connected");
            _socket.Emit("join", _type.ToString());
        };


        _socket.OnUnityThread("initiateConnection", data => {
            var senderId = data.GetValue<string>();

            var newId = StartConnection(senderId);
            
            _peerSocketIds.Add(senderId, newId);
            
            StartCoroutine(_peers[newId].StartConnection(senderId));
        });
        
        _socket.OnUnityThread("sessionDescriptionOffer", data => {
            var senderId = data.GetValue<string>(0);
            var desc = new RTCSessionDescription {
                sdp = data.GetValue<string>(1),
                type = RTCSdpType.Offer
            };
            
            var newId = StartConnection(senderId);
            _peerSocketIds.Add(senderId, newId);
            
            StartCoroutine(_peers[newId].OnSessionDescriptionReceived(senderId, desc));
        });
        
        _socket.OnUnityThread("sessionDescriptionAnswer", data => {
            var senderId = data.GetValue<string>(0);
            var desc = new RTCSessionDescription {
                sdp = data.GetValue<string>(1),
                type = RTCSdpType.Answer
            };
            
            StartCoroutine(_peers[_peerSocketIds[senderId]].OnAnswerReceived(desc));
        });

        _socket.OnUnityThread("iceCandidate", data => {
            var senderId = data.GetValue<string>();
            var iceCandidateInit = new RTCIceCandidateInit {
                candidate = data.GetValue<string>(1),
                sdpMid = data.GetValue<string>(2),
                sdpMLineIndex = data.GetValue<int>(3)
            };
            
            _peers[_peerSocketIds[senderId]].ReceiveIceCandidate(iceCandidateInit);
        });
    }

    private ulong StartConnection(string id) {
        var newId = GetMlAPIClientId(NextId);
        Debug.Log(newId);
        _peers[newId] = new WebRtcConnection(_socket, this, newId);

        return newId;
    }

    public override bool StartClient() {
        _type = Type.Client;

        StartSocket();

        return true;
    }

    public override bool StartServer() {
        _type = Type.Server;

        StartSocket();

        return true;
    }

    public override void DisconnectRemoteClient(ulong clientId) {
        // throw new NotImplementedException();
        Log($"disconnect {clientId}");
    }

    public override void DisconnectLocalClient() {
        // throw new NotImplementedException();
        Log("disconnect local");
    }

    public override ulong GetCurrentRtt(ulong clientId) {
        // throw new NotImplementedException();
        return 1;
    }

    public override void Shutdown() {
        Log("Shutdown");
        _socket?.Disconnect();
        foreach (var (id, connection) in _peers) {
            connection.Close();
        }
    }

    public override void Initialize(NetworkManager networkManager = null) {
        // throw new NotImplementedException();
    }

    public override ulong ServerClientId => 0;

    public override void Send(ulong clientId, ArraySegment<byte> data, NetworkDelivery delivery) {
        Debug.Log(string.Join(", ", _peers.Keys));
        _peers[clientId].SendMessage(data);
    }

    public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime) {
        clientId = 0;
        receiveTime = Time.realtimeSinceStartup;
        payload = new ArraySegment<byte>();
        return NetworkEvent.Nothing;
    }

    public void ProcessEvent(NetworkEvent eventType, WebRtcConnection peer, ArraySegment<byte> payload,
        float receiveTime) {
        InvokeOnTransportEvent(eventType, peer.id, payload, receiveTime);
    }

    private ulong GetMlAPIClientId(ulong clientId) {

        if (_type == Type.Server) {
            clientId += 1;
        }

        return clientId;
    }

    public void Log(object message) {
        if (logNetworkDebug) Debug.Log(message);
    }

    [Serializable]
    public struct DescriptionData {
        public RTCSdpType type;
        public string desc;
    }
}