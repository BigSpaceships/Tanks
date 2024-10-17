using System;
using System.Runtime.InteropServices;
using AOT;
using Unity.Netcode;
using UnityEngine;

public class WebGLWebRTCTransport : WebRTCTransportBase
{
    private static WebGLWebRTCTransport _instance;

    public WebGLWebRTCTransport(WebRTCTransport transport) : base(transport) {
    }

    [DllImport("__Internal")]
    private static extern void ConnectSocket(string str, string type);

    [DllImport("__Internal")]
    private static extern void SendData(uint id, byte[] data, int dataLength);

    private delegate void OnMessageCallback(IntPtr messagePrt, int messageSize);
    [DllImport("__Internal")]
    private static extern void SetOnMessage(OnMessageCallback callback);

    [DllImport("__Internal")]
    private static extern void DisconnectSelf();

    [DllImport("__Internal")]
    private static extern void DisconnectRemoteClient(int id);

    [DllImport("__Internal")]
    private static extern void CloseSocket();

    [MonoPInvokeCallback(typeof(OnMessageCallback))]
    private static void OnMessageEvent(IntPtr messagePrt, int messageSize) {
        var buffer = new byte[messageSize];

        Marshal.Copy(messagePrt, buffer, 0, messageSize);

        var typeInt = buffer[0];
        var type = (NetworkEvent)typeInt;

        var id = BitConverter.ToUInt64(buffer, 1);

        var dataView = new ArraySegment<byte>(buffer, 9, messageSize - 9);

        Debug.Log("received data with type " + type);

        _instance.Transport.TransportEvent(type, id, dataView, Time.time);
    }

    protected override void ConnectSocket(string serverUri) {
        SetOnMessage(OnMessageEvent);
        ConnectSocket(serverUri, _type.ToString());
    }

    public override void SendData(ulong id, ArraySegment<byte> data) {
        Debug.Log("sending data");
        SendData((uint) id, data.ToArray(), data.Count);
    }

    public override void DisconnectLocal() {
        DisconnectSelf();
    }

    public override void DisconnectRemote(ulong id) {
        DisconnectRemoteClient((int)id);
    }

    public override void Close() {
        CloseSocket();
    }

    public override void Initialize() {
        _instance = this;
    }
}
