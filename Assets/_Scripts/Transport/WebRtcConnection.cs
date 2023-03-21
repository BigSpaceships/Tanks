using System;
using System.Collections;
using Unity.Netcode;
using Unity.WebRTC;
using UnityEngine;

public class WebRtcConnection {
    private readonly SocketIOUnity _socket;
    private WebRtcTransport _transport;

    private readonly RTCPeerConnection _pc;
    private RTCDataChannel _dataChannel;

    private DelegateOnDataChannel _onDataChannel;
    private DelegateOnMessage _onDataChannelMessage;

    public void SendMessage(ArraySegment<byte> data) {
        _dataChannel.Send(data.Array);
    }

    private void ReceiveMessage(byte[] data) {
        // _transport.ProcessEvent(NetworkEvent.Data, );
    }

    public WebRtcConnection(SocketIOUnity socket, WebRtcTransport transport) {
        _socket = socket;
        _transport = transport;

        _socket.OnUnityThread("sessionDescription", data => {
            var desc = data.GetValue<string>();
            var type = (RTCSdpType)data.GetValue<int>(1);

            switch (type) {
                case RTCSdpType.Offer:
                    NetworkManager.Singleton.StartCoroutine(OnSessionDescriptionReceived(desc));
                    break;
                case RTCSdpType.Answer:
                    NetworkManager.Singleton.StartCoroutine(OnAnswerReceived(desc));
                    break;
            }
        });

        _socket.OnUnityThread("iceCandidate", data => {
            var iceCandidateInit = new RTCIceCandidateInit {
                candidate = data.GetValue<string>(),
                sdpMid = data.GetValue<string>(1),
                sdpMLineIndex = data.GetValue<int>(2)
            };
            ReceiveIceCandidate(iceCandidateInit);
        });

        var config = GetConfig();

        _transport.Log("Creating local RTCPeerConnection");

        _pc = new RTCPeerConnection(ref config);

        _pc.OnIceCandidate = OnIceCandidate;
        _pc.OnIceConnectionChange = OnIceConnectionChange;

        _pc.OnConnectionStateChange = OnConnectionStateChange;

        _pc.OnDataChannel = channel => {
            _dataChannel = channel;
            _dataChannel.OnMessage = ReceiveMessage;
        };
    }

    public void Close() {
        _pc.Close();
    }

    public IEnumerator StartConnection() {
        _transport.Log("hello");
        RTCDataChannelInit config = new RTCDataChannelInit();
        _dataChannel = _pc.CreateDataChannel("data", config);

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
        _socket.Emit("sessionDescription", localDesc.sdp, localDesc.type);
    }

    private IEnumerator OnSessionDescriptionReceived(string desc) {
        var remoteDesc = new RTCSessionDescription { sdp = desc, type = RTCSdpType.Offer };

        _transport.Log($"Received remote description {desc}");
        _transport.Log("Setting remote description");

        var remoteDescOp = _pc.SetRemoteDescription(ref remoteDesc);
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
        _socket.Emit("sessionDescription", answerDesc.sdp, answerDesc.type);
    }

    private IEnumerator OnAnswerReceived(string desc) {
        _transport.Log($"Received answer \n{desc}");

        var remoteDesc = new RTCSessionDescription { sdp = desc, type = RTCSdpType.Answer };
        var remoteDescOp = _pc.SetRemoteDescription(ref remoteDesc);

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
        _socket.Emit("iceCandidate", candidate.Candidate, candidate.SdpMid, candidate.SdpMLineIndex);
        _transport.Log($"remote ICE Candidate: {candidate}");
    }

    private void ReceiveIceCandidate(RTCIceCandidateInit candidateInit) {
        var iceCandidate = new RTCIceCandidate(candidateInit);
        _pc.AddIceCandidate(iceCandidate);

        _transport.Log($"Added new ice candidate {candidateInit.candidate}");
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