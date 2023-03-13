using System;
using SocketIOClient;
using Unity.Netcode;
using UnityEngine;

public class WebRtcTransport : NetworkTransport {
    private SocketIOUnity _socket;

    private enum Type {
        Server,
        Client,
    }

    private Type _type;

    private void StartSocket() {
        Debug.Log(_type);
        var uri = new Uri("https://TanksSignalingServer.bigspaceships.repl.co");
        _socket = new SocketIOUnity(uri, new SocketIOOptions {
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        });

        _socket.Connect();

        _socket.OnConnected += (sender, args) => { _socket.Emit("type", _type.ToString()); };
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
    }

    public override void DisconnectLocalClient() {
        // throw new NotImplementedException();
    }

    public override ulong GetCurrentRtt(ulong clientId) {
        // throw new NotImplementedException();
        return 0;
    }

    public override void Shutdown() {
        // throw new NotImplementedException();
    }

    public override void Initialize(NetworkManager networkManager = null) {
        // throw new NotImplementedException();
    }

    public override ulong ServerClientId { get; }

    public override void Send(ulong clientId, ArraySegment<byte> data, NetworkDelivery delivery) {
        // throw new NotImplementedException();
    }

    public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload,
        out float receiveTime) {
        clientId = 0;
        receiveTime = Time.realtimeSinceStartup;
        payload = new ArraySegment<Byte>();
        return NetworkEvent.Nothing;
    }
}