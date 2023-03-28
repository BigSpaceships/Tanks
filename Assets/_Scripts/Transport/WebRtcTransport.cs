using System;
using SocketIOClient;
using Unity.Netcode;
using UnityEngine;

public class WebRtcTransport : NetworkTransport {
    private SocketIOUnity _socket;
    private WebRtcConnection _webRtcConnection;

    [SerializeField] private bool logNetworkDebug;

    private enum Type {
        Server,
        Client,
    }

    private ulong _id;
    private ulong _lastId = 0;
    private ulong NextId => _lastId++;

    private Type _type;

    private void StartSocket() {
        var uri = new Uri("https://8080-bigspaceshi-tanksignals-itvkcauzscy.ws-us92.gitpod.io/");
        _socket = new SocketIOUnity(uri, new SocketIOOptions {
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        });

        _socket.Connect();

        _socket.OnConnected += (sender, args) => {
            Log("Socket connected");
            _socket.Emit("type", _type.ToString());
        };

        _webRtcConnection = new WebRtcConnection(_socket, this, NextId);

        _socket.OnUnityThread("initiateConnection", data => { StartCoroutine(_webRtcConnection.StartConnection()); });
    }

    public override bool StartClient() {
        _type = Type.Client;

        StartSocket();

        return true;
    }

    public override bool StartServer() {
        _type = Type.Server;

        _id = 1; // TODO: Cursed AF

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
        _webRtcConnection?.Close();
    }

    public override void Initialize(NetworkManager networkManager = null) {
        // throw new NotImplementedException();
    }

    public override ulong ServerClientId => 0;

    public override void Send(ulong clientId, ArraySegment<byte> data, NetworkDelivery delivery) {
        _webRtcConnection.SendMessage(data);
    }

    public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime) {
        clientId = 0;
        receiveTime = Time.realtimeSinceStartup;
        payload = new ArraySegment<byte>();
        return NetworkEvent.Nothing;
    }

    public void ProcessEvent(NetworkEvent eventType, WebRtcConnection peer, ArraySegment<byte> payload,
        float receiveTime) {
        InvokeOnTransportEvent(eventType, GetMlapiClientId(peer), payload, receiveTime);
    }

    ulong GetMlapiClientId(WebRtcConnection peer) {
        ulong clientId = (ulong)peer.id;

        if (_type == Type.Server) {
            clientId += 1;
        }

        return clientId;
    }

    public void Log(object message) {
        if (logNetworkDebug) Debug.Log(message);
    }
}