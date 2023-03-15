using System.Collections;
using Unity.Netcode;
using Unity.WebRTC;
using UnityEngine;

public class WebRtcConnection {
    private SocketIOUnity _socket;

    private RTCPeerConnection _pc;
    private RTCDataChannel _dataChannel;

    private DelegateOnDataChannel _onDataChannel;
    private DelegateOnMessage _onDataChannelMessage;

    public WebRtcConnection(SocketIOUnity socket) {
        _socket = socket;

        var config = GetConfig();

        Debug.Log("Creating local RTCPeerConnection");

        _pc = new RTCPeerConnection(ref config);

        _pc.OnIceCandidate = OnIceCandidate;
        _pc.OnIceConnectionChange = OnIceConnectionChange;

        _pc.OnDataChannel = channel => {
            _dataChannel = channel;
            _dataChannel.OnMessage = _onDataChannelMessage;
        };

        NetworkManager.Singleton.StartCoroutine(StartConnection());
    }

    private IEnumerator StartConnection() {
        RTCDataChannelInit config = new RTCDataChannelInit();
        _dataChannel = _pc.CreateDataChannel("data", config);

        var createOfferOp = _pc.CreateOffer();
        yield return createOfferOp;

        if (createOfferOp.IsError) {
            Debug.LogError(createOfferOp.Error.message);
            yield break;
        }

        var localDesc = createOfferOp.Desc;
        var localDescOp = _pc.SetLocalDescription(ref localDesc);
        yield return localDescOp;

        if (localDescOp.IsError) {
            Debug.LogError(localDescOp);
            yield break;
        }

        _socket.Emit("sessionDescription", localDesc.sdp, localDesc.type);
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
        Debug.Log($"remote ICE Candidate: {candidate}");
    }

    void OnIceConnectionChange(RTCIceConnectionState state) // just log ig
    {
        switch (state) {
            case RTCIceConnectionState.New:
                Debug.Log("IceConnectionState: New");
                break;
            case RTCIceConnectionState.Checking:
                Debug.Log("IceConnectionState: Checking");
                break;
            case RTCIceConnectionState.Closed:
                Debug.Log("IceConnectionState: Closed");
                break;
            case RTCIceConnectionState.Completed:
                Debug.Log("IceConnectionState: Completed");
                break;
            case RTCIceConnectionState.Connected:
                Debug.Log("IceConnectionState: Connected");
                break;
            case RTCIceConnectionState.Disconnected:
                Debug.Log("IceConnectionState: Disconnected");
                break;
            case RTCIceConnectionState.Failed:
                Debug.Log("IceConnectionState: Failed");
                break;
            case RTCIceConnectionState.Max:
                Debug.Log("IceConnectionState: Max");
                break;
            default:
                break;
        }
    }
}