#if !UNITY_WEBGL || UNITY_EDITOR

using System;
using System.Collections;
using Unity.Netcode;
using Unity.WebRTC;
using UnityEngine;

public class NativeWebRTCConnection {
    private readonly SocketIOUnity _socket;
    private NativeWebRTCTransport _transport;

    private readonly RTCPeerConnection _pc;
    private RTCDataChannel _dataChannel;

    private DelegateOnDataChannel _onDataChannel;
    private DelegateOnMessage _onDataChannelMessage;

    public ulong id { get; }
    private string _otherSocketId;

    public void SendMessage(ArraySegment<byte> data) {
        _dataChannel.Send(data.ToArray());
    }

    private void ReceiveMessage(byte[] data) {
        _transport.ProcessEvent(NetworkEvent.Data, this, new ArraySegment<byte>(data), Time.realtimeSinceStartup);
    }

    public NativeWebRTCConnection(SocketIOUnity socket, NativeWebRTCTransport transport, ulong id) {
        _socket = socket;
        _transport = transport;
        this.id = id;

        var config = GetConfig();

        _transport.Log("Creating local RTCPeerConnection");

        _pc = new RTCPeerConnection(ref config);

        _pc.OnIceCandidate = OnIceCandidate;
        _pc.OnIceConnectionChange = OnIceConnectionChange;

        _pc.OnConnectionStateChange = OnConnectionStateChange;

        _pc.OnDataChannel = channel => {
            _dataChannel = channel;
            _dataChannel.OnMessage = ReceiveMessage;

            _dataChannel.OnClose = OnDataChannelClosed;

            OnDataChannelOpen();
        };

        _pc.OnTrack = track => Debug.Log(track.ToString());
    }

    public void Close() {
        _dataChannel.Close();
        _pc.Close();
    }

    public IEnumerator StartConnection(string otherId) {
        _otherSocketId = otherId;

        RTCDataChannelInit config = new RTCDataChannelInit();
        _dataChannel = _pc.CreateDataChannel("data", config);

        _dataChannel.OnMessage = ReceiveMessage;
        _dataChannel.OnOpen = OnDataChannelOpen;
        _dataChannel.OnClose = OnDataChannelClosed;

        _transport.Log("Creating offer");
        var createOfferOp = _pc.CreateOffer();
        yield return createOfferOp;

        if (createOfferOp.IsError) {
            Debug.LogError(createOfferOp.Error.message);
            yield break;
        }

        _transport.Log("Setting local description");
        var localDesc = createOfferOp.Desc;
        var localDescOp = _pc.SetLocalDescription(ref localDesc);
        yield return localDescOp;

        if (localDescOp.IsError) {
            Debug.LogError(localDescOp.Error.message);
            yield break;
        }

        _transport.Log($"Local description: \n{localDesc.sdp}");
        _socket.Emit("sessionDescriptionOffer", _otherSocketId, localDesc.sdp);
    }

    public IEnumerator OnSessionDescriptionReceived(string otherId, RTCSessionDescription desc) {
        _otherSocketId = otherId;

        _transport.Log($"Received remote description {desc}");
        _transport.Log("Setting remote description");

        var remoteDescOp = _pc.SetRemoteDescription(ref desc);
        yield return remoteDescOp;

        if (remoteDescOp.IsError) {
            Debug.LogError(remoteDescOp.Error.message);
            yield break;
        }

        _transport.Log("Creating Answer");
        var createAnswerOp = _pc.CreateAnswer();
        yield return createAnswerOp;

        if (createAnswerOp.IsError) {
            Debug.LogError(createAnswerOp.Error.message);
            yield break;
        }

        _transport.Log("Setting local description");
        var answerDesc = createAnswerOp.Desc;
        var localDescOp = _pc.SetLocalDescription(ref answerDesc);
        yield return localDescOp;

        if (localDescOp.IsError) {
            Debug.LogError(localDescOp.Error.message);
            yield break;
        }

        _transport.Log($"Sending answer {answerDesc.sdp}");
        _socket.Emit("sessionDescriptionAnswer", _otherSocketId, answerDesc.sdp);
    }

    public IEnumerator OnAnswerReceived(RTCSessionDescription desc) {
        _transport.Log($"Received answer \n{desc}");

        var remoteDescOp = _pc.SetRemoteDescription(ref desc);

        yield return remoteDescOp;

        if (remoteDescOp.IsError) {
            Debug.LogError(remoteDescOp.Error.message);
            yield break;
        }

        _transport.Log("Successfully set remote description");
    }

    private RTCConfiguration GetConfig() {
        RTCConfiguration config = default;

        config.iceServers = new RTCIceServer[] {
            new RTCIceServer { urls = new string[] { "stun:stun.l.google.com:19302" } }
        };

        return config;
    }

    private void OnIceCandidate(RTCIceCandidate candidate) {
        _socket.Emit("iceCandidate", _otherSocketId, candidate.Candidate, candidate.SdpMid, candidate.SdpMLineIndex);
        _transport.Log($"remote ICE Candidate: {candidate}");
    }

    public void ReceiveIceCandidate(RTCIceCandidateInit candidateInit) {
        var iceCandidate = new RTCIceCandidate(candidateInit);
        _pc.AddIceCandidate(iceCandidate);

        _transport.Log($"Added new ice candidate {candidateInit.candidate}");
    }

    private void OnDataChannelOpen() {
        _transport.ProcessEvent(NetworkEvent.Connect, this, default, Time.realtimeSinceStartup);
    }

    private void OnDataChannelClosed() {
        _transport.Log("Close");
        _transport.ProcessEvent(NetworkEvent.Disconnect, this, default, Time.realtimeSinceStartup);
    }

    private void OnConnectionStateChange(RTCPeerConnectionState state) {
        _transport.Log($"PeerConnectionState: {state.ToString()}");
    }

    private void OnIceConnectionChange(RTCIceConnectionState state) // just log ig
    {
        switch (state) {
            case RTCIceConnectionState.New:
                _transport.Log("IceConnectionState: New");
                break;
            case RTCIceConnectionState.Checking:
                _transport.Log("IceConnectionState: Checking");
                break;
            case RTCIceConnectionState.Closed:
                _transport.Log("IceConnectionState: Closed");
                break;
            case RTCIceConnectionState.Completed:
                _transport.Log("IceConnectionState: Completed");
                break;
            case RTCIceConnectionState.Connected:
                _transport.Log("IceConnectionState: Connected");
                break;
            case RTCIceConnectionState.Disconnected:
                _transport.Log("IceConnectionState: Disconnected");
                break;
            case RTCIceConnectionState.Failed:
                _transport.Log("IceConnectionState: Failed");
                break;
            case RTCIceConnectionState.Max:
                _transport.Log("IceConnectionState: Max");
                break;
            default:
                break;
        }
    }
}

#endif