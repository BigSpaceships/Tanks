
var WebRTCLib = {
    $state: {
        onMessage: null,
        socket: null,
        peers: null,
        peerIds: null,
        type: null,
        id: 0,
    },
    ConnectSocket: function (url, type) {
        state.socket = io(UTF8ToString(url));

        state.type = UTF8ToString(type);

        state.peers = new Map();
        state.peerIds = new Map();

        state.socket.on("connect", () => {
            state.socket.emit("join", state.type)
        });

        const getMlAPIClientId = function (id) {
            if (state.type == "Server") {
                return id + 1;
            }

            return id;
        };

        const WebRTCConnection = class {
            constructor(socket, id) {
                this.socket = socket;

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
            };
            setOnMessage(callback) {
                this.onmessage = callback;
            };
            dataChannelMessage(event) {
                console.log("got message with data");
                this.onmessage({
                    type: 0,
                    id: this.id,
                    data: event.data,
                });
            };
            dataChannelOpen() {
                this.onmessage({
                    type: 1,
                    id: this.id,
                    data: new Uint8Array(),
                });
            };
            dataChannelClose() {
                this.onmessage({
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

        const startConnection = function() {
            const newId = getMlAPIClientId(state.id++);

            const connection = new WebRTCConnection(state.socket, newId);

            const longToByteArray = function (long) {
                // we want to represent the input as a 8-bytes array
                var byteArray = [0, 0, 0, 0, 0, 0, 0, 0];

                for ( var index = 0; index < byteArray.length; index ++ ) {
                    var byte = long & 0xff;
                    byteArray [ index ] = byte;
                    long = (long - byte) / 256 ;
                }

                return byteArray;
            };

            connection.setOnMessage((event) => {
                var dataOffset = 1 + 8;

                var data = new Uint8Array(dataOffset + event.data.byteLength);

                data.set(new Uint8Array(event.data), dataOffset);
                data.set([event.type], 0);
                data.set(longToByteArray(event.id), 1);

                var buffer = _malloc(data.length);
                HEAPU8.set(data, buffer);

                try {
                    Module['dynCall_vii'](state.onMessage, buffer, data.length);
                } finally {
                    _free(buffer);
                }
            });

            state.peers.set(newId, connection);

            return newId;
        };

        const initiateConnection = function (...data) {
            console.log("new connection");

            var newId = startConnection();

            state.peerIds.set(data[0], newId);

            state.peers.get(newId).startConnection(data[0]);
        };

        const sessionDescriptionOffer = function (...data) {
            console.log("new offer");

            var newId = startConnection();

            state.peerIds.set(data[0], newId);

            state.peers.get(newId).recieveOffer(data[0], data[1]);
        };

        const sessionDescriptionAnswer = function (...data) {
            console.log("new answer");
            state.peers.get(state.peerIds.get(data[0])).recieveAnswer(data[1]);
        };

        const iceCandidate = function (...data) {
            console.log("ice candidate");
            var newCandidate = {
                candidate: data[1],
                sdpMid: data[2],
                sdpMLineIndex: data[3],
            }

            state.peers.get(state.peerIds.get(data[0])).recieveIceCandidate(newCandidate);
        };

        state.socket.on("initiateConnection", initiateConnection);
        state.socket.on("sessionDescriptionOffer", sessionDescriptionOffer);
        state.socket.on("sessionDescriptionAnswer", sessionDescriptionAnswer);
        state.socket.on("iceCandidate", iceCandidate);
    },
    SendData: function (id, data, size) {
        var dataView = HEAPU8.subarray(data, data + size);
        console.log("sending data js");
        state.peers.get(id).sendData(dataView);
    },
    SetOnMessage: function (callback) {
        state.onMessage = callback;
    },
    DisconnectRemoteClient: function (id) {
        state.peers.get(id).disconnect();
        state.peers.delete(id);
    },
    DisconnectSelf: function () {
        state.peers.entries().forEach((peer) => {
            peer.disconnect();
        });

        state.peers = {};
        state.peerIds = {};
    },
    CloseSocket: function () {
        state.socket.close();
    }
}

autoAddDeps(WebRTCLib, '$state');
mergeInto(LibraryManager.library, WebRTCLib);