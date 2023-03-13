using System;
using System.Collections;
using SocketIOClient;
using Unity.WebRTC;
using UnityEngine;

[ExecuteAlways]
public class ConnectionTesting : MonoBehaviour {
    public SocketIOUnity socket;

    private RTCPeerConnection localPC;
    private RTCDataChannel dataChannel;

    private DelegateOnIceCandidate localOnIceCandidate;

    private DelegateOnIceConnectionChange localOnIceConnectionChange;

    private DelegateOnDataChannel onDataChannel;
    private DelegateOnMessage onDataChannelMessage;

    public void Setup() {
        localOnIceCandidate = candidate => { OnIceCandidate(localPC, candidate); };

        localOnIceConnectionChange = state => { OnIceConnectionChange(localPC, state); };

        onDataChannel = channel => {
            dataChannel = channel;
            dataChannel.OnMessage = onDataChannelMessage;
        };

        onDataChannelMessage = bytes => { Debug.Log(System.Text.Encoding.UTF8.GetString(bytes)); };
    }

    public void ConnectSignalingServer() {
        DisconnectSignalingServer();

        var uri = new Uri("https://TanksSignalingServer.bigspaceships.repl.co");
        socket = new SocketIOUnity(uri, new SocketIOOptions {
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        });

        socket.Connect();

        socket.OnConnected += (sender, args) => {
            var senderSocket = (SocketIOUnity)sender;
            socket.Emit("type", "unity");
        };

        socket.On("test", data => { Debug.Log(data.GetValue<string>()); });

        socket.OnUnityThread("join", data => { StartCoroutine(StartConnection()); });

        socket.OnUnityThread("sessionDescription", data => {
            var type = (RTCSdpType)data.GetValue<int>(1);
            var desc = data.GetValue<string>();

            Debug.Log(desc);

            if (type == RTCSdpType.Offer) {
                StartCoroutine(OnSessionDescriptionReceived(desc));
            }

            if (type == RTCSdpType.Answer) {
                StartCoroutine(OnAnswerReceived(desc));
            }
        });

        socket.OnUnityThread("iceCandidate", data => {
            var iceCandidateInit = new RTCIceCandidateInit {
                candidate = data.GetValue<string>(0),
                sdpMid = data.GetValue<string>(1),
                sdpMLineIndex = data.GetValue<int>(2)
            };
            receiveIceCandidate(iceCandidateInit);
        });
    }

    public void CreateLocalPC() {
        Setup();

        Debug.Log("joining");
        var config = GetConfig();

        Debug.Log("Creating local RTCPeerConnection");
        localPC = new RTCPeerConnection(ref config);

        localPC.OnIceCandidate = localOnIceCandidate;
        localPC.OnIceConnectionChange = localOnIceConnectionChange;

        localPC.OnDataChannel = onDataChannel;
    }

    public IEnumerator StartConnection() {
        CreateLocalPC();

        RTCDataChannelInit config = new RTCDataChannelInit();
        dataChannel = localPC.CreateDataChannel("data", config);

        Debug.Log("Creating offer");
        var createOfferOp = localPC.CreateOffer();
        yield return createOfferOp;

        if (createOfferOp.IsError) {
            Debug.LogError(createOfferOp.Error.message);
            yield break;
        }

        Debug.Log("Setting local description");
        var localDesc = createOfferOp.Desc;
        var localDescriptionOp = localPC.SetLocalDescription(ref localDesc);
        yield return localDescriptionOp;

        if (localDescriptionOp.IsError) {
            Debug.LogError(localDescriptionOp.Error.message);
            yield break;
        }

        Debug.Log($"Local description: \n{localDesc.sdp}");
        socket.Emit("sessionDescription", localDesc.sdp, localDesc.type);
    }

    public IEnumerator OnSessionDescriptionReceived(string desc) {
        CreateLocalPC();

        var remoteDesc = new RTCSessionDescription { sdp = desc, type = RTCSdpType.Offer };

        Debug.Log($"Received remote description {desc}");
        Debug.Log("Setting remote description");
        var remoteDescriptionOp = localPC.SetRemoteDescription(ref remoteDesc);
        yield return remoteDescriptionOp;

        if (remoteDescriptionOp.IsError) {
            Debug.LogError(remoteDescriptionOp.Error.message);
            yield break;
        }

        Debug.Log("Creating Answer");
        var createAnswerOp = localPC.CreateAnswer();
        yield return createAnswerOp;

        if (createAnswerOp.IsError) {
            Debug.Log(createAnswerOp.Error.message);
            yield break;
        }

        Debug.Log("Setting local description");
        var answerDescription = createAnswerOp.Desc;
        var localDescriptionOp = localPC.SetLocalDescription(ref answerDescription);
        yield return localDescriptionOp;

        if (localDescriptionOp.IsError) {
            Debug.LogError(localDescriptionOp.Error.message);
            yield break;
        }

        Debug.Log($"Sending answer {answerDescription.sdp}");
        socket.Emit("sessionDescription", answerDescription.sdp, answerDescription.type);
    }

    public IEnumerator OnAnswerReceived(string desc) {
        Debug.Log($"Received answer \n{desc}");

        var remoteDescription = new RTCSessionDescription { sdp = desc, type = RTCSdpType.Answer };
        var remoteDescriptionOp = localPC.SetRemoteDescription(ref remoteDescription);

        yield return remoteDescriptionOp;

        if (remoteDescriptionOp.IsError) {
            Debug.LogError(remoteDescriptionOp.Error.message);
            yield break;
        }

        Debug.Log("Successfully set remote description");
    }

    public void SendMessage() {
        socket.Emit("test", "HELOOOO");
    }

    public void DisconnectSignalingServer() {
        if (socket is not { Connected: true }) return;

        Debug.Log("disconnecting");
        socket.Disconnect();
        socket.Dispose();
    }

    public void DisconnectRtc() {
        localPC?.Dispose();
    }

    private void OnDisable() {
        DisconnectSignalingServer();
        DisconnectRtc();
    }

    private RTCConfiguration GetConfig() {
        RTCConfiguration config = default;

        config.iceServers = new RTCIceServer[] {
            new RTCIceServer { urls = new string[] { "stun:stun.l.google.com:19302" } }
        };

        return config;
    }

    private void OnIceCandidate(RTCPeerConnection pc, RTCIceCandidate candidate) {
        // pc.AddIceCandidate(new RTCIceCandidate(candidate.))
        socket.Emit("iceCandidate", candidate.Candidate, candidate.SdpMid, candidate.SdpMLineIndex);
        Debug.Log($"remote ICE Candidate: {candidate}");
    }

    private void receiveIceCandidate(RTCIceCandidateInit candidateInit) {
        var iceCandidate = new RTCIceCandidate(candidateInit);
        localPC.AddIceCandidate(iceCandidate);

        Debug.Log($"Added new ice candidate {candidateInit.candidate}");
    }

    string GetName(RTCPeerConnection pc) {
        return (pc == localPC) ? "local" : "remote";
    }

    void OnIceConnectionChange(RTCPeerConnection pc, RTCIceConnectionState state) // just log ig
    {
        switch (state) {
            case RTCIceConnectionState.New:
                Debug.Log($"{GetName(pc)} IceConnectionState: New");
                break;
            case RTCIceConnectionState.Checking:
                Debug.Log($"{GetName(pc)} IceConnectionState: Checking");
                break;
            case RTCIceConnectionState.Closed:
                Debug.Log($"{GetName(pc)} IceConnectionState: Closed");
                break;
            case RTCIceConnectionState.Completed:
                Debug.Log($"{GetName(pc)} IceConnectionState: Completed");
                break;
            case RTCIceConnectionState.Connected:
                Debug.Log($"{GetName(pc)} IceConnectionState: Connected");
                break;
            case RTCIceConnectionState.Disconnected:
                Debug.Log($"{GetName(pc)} IceConnectionState: Disconnected");
                break;
            case RTCIceConnectionState.Failed:
                Debug.Log($"{GetName(pc)} IceConnectionState: Failed");
                break;
            case RTCIceConnectionState.Max:
                Debug.Log($"{GetName(pc)} IceConnectionState: Max");
                break;
            default:
                break;
        }
    }
}