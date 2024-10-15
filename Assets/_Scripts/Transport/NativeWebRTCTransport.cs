
using System;
using System.Collections.Generic;
using SocketIOClient;
using Unity.Netcode;
using Unity.WebRTC;
using UnityEngine;

public class NativeWebRTCTransport : WebRTCTransportBase {
    private SocketIOUnity _socket;

    public WebRTCTransport Transport;

    private ulong _lastId = 0;
    private ulong NextId => _lastId++;

    private readonly Dictionary<ulong, WebRtcConnection> _peers = new();
    private readonly Dictionary<string, ulong> _peerSocketIds = new();

    public NativeWebRTCTransport(WebRTCTransport transport) {
        Transport = transport;
    }

    protected override void ConnectSocket(string serverUri) {
        var uri = new Uri(serverUri);
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

            Transport.StartCoroutine(_peers[newId].StartConnection(senderId));
        });

        _socket.OnUnityThread("sessionDescriptionOffer", data => {
            var senderId = data.GetValue<string>(0);
            var desc = new RTCSessionDescription {
                sdp = data.GetValue<string>(1),
                type = RTCSdpType.Offer
            };

            var newId = StartConnection();
            _peerSocketIds.Add(senderId, newId);

            Transport.StartCoroutine(_peers[newId].OnSessionDescriptionReceived(senderId, desc));
        });

        _socket.OnUnityThread("sessionDescriptionAnswer", data => {
            var senderId = data.GetValue<string>(0);
            var desc = new RTCSessionDescription {
                sdp = data.GetValue<string>(1),
                type = RTCSdpType.Answer
            };

            Transport.StartCoroutine(_peers[_peerSocketIds[senderId]].OnAnswerReceived(desc));
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

    public override void SendData(ulong id, ArraySegment<byte> data) {
        _peers[id].SendMessage(data);
    }

    public void ProcessEvent(NetworkEvent eventType, WebRtcConnection peer, ArraySegment<byte> payload,
        float receiveTime) {
        if (eventType == NetworkEvent.Disconnect) {
            _peers.Remove(peer.id);
        }

        Transport.TransportEvent(eventType, peer.id, payload, receiveTime);
    }

    public override void DisconnectLocal() {
        foreach (var peerPair in _peers) {
            peerPair.Value.Close();
        }

        _peers.Clear();
        _peerSocketIds.Clear();

        Log("disconnect local");
    }

    public override void DisconnectRemote(ulong id) {
        if (_peers.ContainsKey(id)) {
            _peers[id].Close();
            _peers.Remove(id);

            var keysToRemove = new List<string>();

            foreach (var kv in _peerSocketIds) {
                if (id == kv.Value) keysToRemove.Add(kv.Key);
            }

            foreach (var key in keysToRemove) {
                _peerSocketIds.Remove(key);
            }

            Log($"disconnect {id}");
        }
    }

    public override void Close() {
        Log("Shutdown");
        _socket?.Disconnect();
        _socket?.Dispose();
        foreach (var (id, connection) in _peers) {
            connection.Close();
        }

        _peers.Clear();

        _lastId = 0;
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
