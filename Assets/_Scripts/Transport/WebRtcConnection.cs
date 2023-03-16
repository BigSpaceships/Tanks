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
        
        _socket.OnUnityThread("sessionDescription", data => {
            var desc = data.GetValue<string>();
            var type = (RTCSdpType) data.GetValue<int>(1);

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
            RecieveIceCandidate(iceCandidateInit);
        });

        var config = GetConfig();

        Debug.Log("Creating local RTCPeerConnection");

        _pc = new RTCPeerConnection(ref config);

        _pc.OnIceCandidate = OnIceCandidate;
        _pc.OnIceConnectionChange = OnIceConnectionChange;

        _pc.OnDataChannel = channel => {
            _dataChannel = channel;
            _dataChannel.OnMessage = _onDataChannelMessage;
        };
    }

    public void Close() {
        _pc.Close();
    }

    public IEnumerator StartConnection() {
        Debug.Log("hello");
        RTCDataChannelInit config = new RTCDataChannelInit();
        _dataChannel = _pc.CreateDataChannel("data", config);

        Debug.Log("Creating offer");
        var createOfferOp = _pc.CreateOffer();
        yield return createOfferOp;

        if (createOfferOp.IsError) {
            Debug.LogError(createOfferOp.Error.message);
            yield break;
        }

        Debug.Log("Setting local description");
        var localDesc = createOfferOp.Desc;
        var localDescOp = _pc.SetLocalDescription(ref localDesc);
        yield return localDescOp;

        if (localDescOp.IsError) {
            Debug.LogError(localDescOp.Error.message);
            yield break;
        }

        Debug.Log($"Local description: \n{localDesc.sdp}");
        _socket.Emit("sessionDescription", localDesc.sdp, localDesc.type);
    }

    private IEnumerator OnSessionDescriptionReceived(string desc) {
        var remoteDesc = new RTCSessionDescription {sdp = desc, type = RTCSdpType.Offer};
        
        Debug.Log($"Received remote description {desc}");
        Debug.Log("Setting remote description");

        var remoteDescOp = _pc.SetRemoteDescription(ref remoteDesc);
        yield return remoteDescOp;

        if (remoteDescOp.IsError) {
            Debug.LogError(remoteDescOp.Error.message);
            yield break;
        }

        Debug.Log("Creating Answer");
        var createAnswerOp = _pc.CreateAnswer();
        yield return createAnswerOp;

        if (createAnswerOp.IsError) {
            Debug.LogError(createAnswerOp.Error.message);
            yield break;
        }

        Debug.Log("Setting local description");
        var answerDesc = createAnswerOp.Desc;
        var localDescOp = _pc.SetLocalDescription(ref answerDesc);
        yield return localDescOp;

        if (localDescOp.IsError) {
            Debug.LogError(localDescOp.Error.message);
            yield break;
        }
        
        Debug.Log($"Sending answer {answerDesc.sdp}");
        _socket.Emit("sessionDescription", answerDesc.sdp, answerDesc.type);
    }

    private IEnumerator OnAnswerReceived(string desc) {
        Debug.Log($"Received answer \n{desc}");

        var remoteDesc = new RTCSessionDescription {sdp = desc, type = RTCSdpType.Answer};
        var remoteDescOp = _pc.SetRemoteDescription(ref remoteDesc);

        yield return remoteDescOp;

        if (remoteDescOp.IsError) {
            Debug.LogError(remoteDescOp.Error.message);
            yield break;
        }
        
        Debug.Log("Successfully set remote description");
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

    private void RecieveIceCandidate(RTCIceCandidateInit candidateInit) {
        var iceCandidate = new RTCIceCandidate(candidateInit);
        _pc.AddIceCandidate(iceCandidate);
        
        Debug.Log($"Added new ice candidate {candidateInit.candidate}");
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