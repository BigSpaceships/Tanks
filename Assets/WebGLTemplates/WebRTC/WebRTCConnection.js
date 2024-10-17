class WebRTCConnection {
    constructor(socket, transport, id) {
        this.socket = socket;
        this.transport = transport;

        this.id = id;

        const configuration = {'iceServers': [{'urls': 'stun:stun.l.google.com:19302'}]}
        const pc = new RTCPeerConnection(configuration);

        this.pc = pc;

        this.pc.addEventListener("icecandidate", event => {
            if (event.candidate) {
                this.socket.emit("iceCandidate", this.otherSocketId, event.candidate.candidate, event.candidate.sdpMid, event.candidate.sdpMLineIndex);
            }
        });

        this.pc.addEventListener("datachannel", event => {
            this.dataChannel = event.channel;

            this.dataChannel.addEventListener("message", (e) => this.dataChannelMessage(e));
            this.dataChannel.addEventListener("open", (e) => this.dataChannelOpen(e));
            this.dataChannel.addEventListener("close", (e) => this.dataChannelClose(e));
        })
    };
    async startConnection(otherId) {
        this.otherSocketId = otherId;

        this.dataChannel = this.pc.createDataChannel("data");

        this.dataChannel.addEventListener("message", (e) => this.dataChannelMessage(e));
        this.dataChannel.addEventListener("open", (e) => this.dataChannelOpen(e));
        this.dataChannel.addEventListener("close", (e) => this.dataChannelClose(e));

        console.log("creating offer");
        let offer = await this.pc.createOffer();

        console.log("setting local description");
        await this.pc.setLocalDescription(offer);

        this.socket.emit("sessionDescriptionOffer", otherId, offer.sdp)
    };
    async recieveOffer(otherId, offer) {
        this.otherSocketId = otherId;

        await this.pc.setRemoteDescription(new RTCSessionDescription({
            type: "offer",
            sdp: offer,
        }));

        console.log("set remote offer");
        console.log("creating answer");

        let answer = await this.pc.createAnswer();

        console.log("setting local description");
        await this.pc.setLocalDescription(answer);

        this.socket.emit("sessionDescriptionAnswer", otherId, answer.sdp);
    }
    async recieveAnswer(answer) {
        await this.pc.setRemoteDescription(new RTCSessionDescription({
            type: "answer",
            sdp: answer
        }));

        console.log("set answer");
    };
    recieveIceCandidate(candidate) {
        this.pc.addIceCandidate(candidate);
    }
    dataChannelMessage(event) {
        this.transport.sendEvent({
            type: 0,
            id: this.id,
            data: event.data,
        });
    };
    dataChannelOpen() {
        this.transport.sendEvent({
            type: 1,
            id: this.id,
            data: new Uint8Array(),
        });
    };
    dataChannelClose() {
        this.transport.sendEvent({
            type: 2,
            id: this.id,
            data: new Uint8Array(),
        });
    };
    sendData(data) {
        this.dataChannel.send(data);
    };
    disconnect() {
        this.dataChannel.close();
    };
}