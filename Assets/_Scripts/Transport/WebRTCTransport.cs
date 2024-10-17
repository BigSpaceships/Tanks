using System;
using Unity.Netcode;
using UnityEngine;

public class WebRTCTransport : NetworkTransport {
    WebRTCTransportBase transport;

    public string signalServerUri;

    public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery) {
        Debug.Log("transport send data");
        transport.SendData(clientId, payload);
    }

    public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime) {
        clientId = 0;
        receiveTime = Time.realtimeSinceStartup;
        payload = new ArraySegment<byte>();
        return NetworkEvent.Nothing;
    }

    public override bool StartClient() {
        transport.ConnectSocket(WebRTCTransportBase.Type.Client, signalServerUri);

        return true;
    }

    public override bool StartServer() {
        transport.ConnectSocket(WebRTCTransportBase.Type.Server, signalServerUri);

        return true;
    }

    public override void DisconnectRemoteClient(ulong clientId) {
        transport.DisconnectRemote(clientId);
    }

    public override void DisconnectLocalClient() {
        transport.DisconnectLocal();
    }

    public override ulong GetCurrentRtt(ulong clientId) {
        return 1;
    }

    public override void Shutdown() {
        transport.Close();
    }

    public override void Initialize(NetworkManager networkManager = null) {
#if (UNITY_WEBGL && !UNITY_EDITOR)
        transport = new WebGLWebRTCTransport(this);
#else
        transport = new NativeWebRTCTransport(this);
#endif
        transport.Initialize();
    }

    public override ulong ServerClientId => 0;

    public void TransportEvent(NetworkEvent type, ulong clientId, ArraySegment<byte> data, float time) {
        Debug.Log("transport event with type " + type + " from client " + clientId);
        InvokeOnTransportEvent(type, clientId, data, time);
    }
}