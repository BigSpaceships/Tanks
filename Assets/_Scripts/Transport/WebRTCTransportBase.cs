
using System;
using UnityEngine;

public abstract class WebRTCTransportBase {
    public enum Type {
        Client,
        Server,
    }

    protected Type _type;

    public bool logNetworkDebug;

    public void ConnectSocket(Type type, string serverUri) {
        _type = type;
        ConnectSocket(serverUri);
    }

    protected abstract void ConnectSocket(string serverUri);

    public abstract void SendData(ulong id, ArraySegment<byte> data);

    public abstract void DisconnectLocal();

    public abstract void DisconnectRemote(ulong id);

    public abstract void Close();
}