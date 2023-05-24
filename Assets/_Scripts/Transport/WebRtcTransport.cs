using System;
using System.Collections.Generic;
using SocketIOClient;
using Unity.Netcode;
using Unity.WebRTC;
using UnityEngine;

public class WebRtcTransport : NetworkTransport {
    private SocketIOUnity _socket;

    public string signalServerUri;

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
        var uri = new Uri(signalServerUri);
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

            var newId = StartConnection();

            _peerSocketIds.Add(senderId, newId);

            StartCoroutine(_peers[newId].StartConnection(senderId));
        });

        _socket.OnUnityThread("sessionDescriptionOffer", data => {
            var senderId = data.GetValue<string>(0);
            var desc = new RTCSessionDescription {
                sdp = data.GetValue<string>(1),
                type = RTCSdpType.Offer
            };

            var newId = StartConnection();
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

    private ulong StartConnection() {
        var newId = GetMlAPIClientId(NextId);
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
        if (_peers.ContainsKey(clientId)) {
            _peers[clientId].Close();
            _peers.Remove(clientId);

            var keysToRemove = new List<string>();

            foreach (var kv in _peerSocketIds) {
                if (clientId == kv.Value) keysToRemove.Add(kv.Key);
            }

            foreach (var key in keysToRemove) {
                _peerSocketIds.Remove(key);
            }

            Log($"disconnect {clientId}");
        }
    }

    public override void DisconnectLocalClient() {
        foreach (var peerPair in _peers) {
            peerPair.Value.Close();
        }

        _peers.Clear();
        _peerSocketIds.Clear();

        Log("disconnect local");
    }

    // TODO: Ping
    public override ulong GetCurrentRtt(ulong clientId) {
        return 1;
    }

    public override void Shutdown() {
        Log("Shutdown");
        _socket?.Disconnect();
        foreach (var (id, connection) in _peers) {
            connection.Close();
        }

        _peers.Clear();
    }

    public override void Initialize(NetworkManager networkManager = null) { }

    public override ulong ServerClientId => 0;

    public override void Send(ulong clientId, ArraySegment<byte> data, NetworkDelivery delivery) {
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
        if (eventType == NetworkEvent.Disconnect) {
            _peers.Remove(peer.id);
        }

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
}