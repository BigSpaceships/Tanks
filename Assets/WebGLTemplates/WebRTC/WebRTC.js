var webrtcConnection = {
    /**
     * initializes and connects the socket with the url
     * @param url the url to connect to
     */
    connectSocket(url, type) {
        this.socket = io(url);

        this.type = type;

        this.startTime = Date.now();

        this.peers = new Map();
        this.peerIds = new Map();

        this.connectEvents = [];

        this.socket.on("connect", () => {
            this.socket.emit("join", type)
        });

        this.socket.on("initiateConnection", (...data) => this.initiateConnection(...data));
        this.socket.on("sessionDescriptionOffer", (...data) => this.sessionDescriptionOffer(...data));
        this.socket.on("sessionDescriptionAnswer", (...data) => this.sessionDescriptionAnswer(...data));
        this.socket.on("iceCandidate", (...data) => this.iceCandidate(...data));
    },

    /**
     * Starts a new connection
     * @returns new id
     */
    startConnection() {
        const newId = this.getMlAPIClientId(this.nextId());

        const connection = new WebRTCConnection(this.socket, this, newId);

        this.peers.set(newId, connection);

        return newId;
    },

    /**
     * initalizes a new connection on the server
     * @param data contains the socket id
     */
    initiateConnection(...data) {
        console.log("new connection");

        var newId = this.startConnection();

        this.peerIds.set(data[0], newId);

        this.peers.get(newId).startConnection(data[0]);
    },

    /**
     * recieve session description offer
     * @param data contains the peer that sent the offer and the session description
     */
    sessionDescriptionOffer(...data) {
        console.log("new offer");

        var newId = this.startConnection();

        this.peerIds.set(data[0], newId);

        this.peers.get(newId).recieveOffer(data[0], data[1]);
    },

    /**
     * recieve session description answer
     * @param data contains the peer that sent the answer and the session description
     */
    sessionDescriptionAnswer(...data) {
        console.log("new answer");
        this.peers.get(this.peerIds.get(data[0])).recieveAnswer(data[1]);
    },

    /**
     * recieve new ice candidate
     * @param data contains the peer that sent the candidate, canidate, sdpMid, and sdpMLineIndex
     */
    iceCandidate(...data) {
        console.log("ice candidate");
        var newCandidate = {
            candidate: data[1],
            sdpMid: data[2],
            sdpMLineIndex: data[3],
        }

        this.peers.get(this.peerIds.get(data[0])).recieveIceCandidate(newCandidate);
    },

    getMlAPIClientId(id) {
        if (this.type == "Server") {
            return id + 1;
        }

        return id;
    },
    /**
     * queue new event
     * @param {{type: int, id: int, data: Uint8Array}} event
     */
    sendEvent(event) {
        var dataOffset = 1 + 8;

        var newBuffer = new Uint8Array(dataOffset + event.data.byteLength);

        newBuffer.set(new Uint8Array(event.data), dataOffset);
        newBuffer.set([event.type], 0);
        newBuffer.set(this.longToByteArray(event.id), 1);

        this.onmessage(newBuffer);
    },
    longToByteArray(long) {
        // we want to represent the input as a 8-bytes array
        var byteArray = [0, 0, 0, 0, 0, 0, 0, 0];

        for ( var index = 0; index < byteArray.length; index ++ ) {
            var byte = long & 0xff;
            byteArray [ index ] = byte;
            long = (long - byte) / 256 ;
        }

        return byteArray;
    },
    sendData(id, data) {
        this.peers.get(id).sendData(data);
    },
    disconnectClient(id) {
        this.peers.get(id).disconnect();
        this.peers.delete(id);
    },
    disconnectSelf() {
        this.peers.entries().forEach((peer) => {
            peer.disconnect();
        });

        this.peers = {};
        this.peerIds = {};
    },
    close() {
        this.socket.close();
    },
    bytesToHex(data) {
        return [...new Uint8Array(data)]
            .map(x => x.toString(16).padStart(2, '0'))
            .join('');
    }
}