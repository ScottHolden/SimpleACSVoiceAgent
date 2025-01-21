(()=>{
    // Helper functions
    function bufferedArray(size, callback) {
        let buffer = new Uint8Array();
        const flush = () => {
            if (buffer.length <= 0) return;
            callback(new Uint8Array(buffer.slice(0, size)));
            buffer = new Uint8Array(buffer.slice(size));
        }
        return {
            addData: (data) => {
                buffer = new Uint8Array([...buffer, ...new Uint8Array(data)]);
                if (buffer.length >= size) flush();
            },
            flush
        };
    }
    function atobInt16(data) {
        const binary = atob(data);
        const bytes = Uint8Array.from(binary, (c) => c.charCodeAt(0));
        return new Int16Array(bytes.buffer);
    }

    const statusElement = document.getElementById('status');
    const connectButton = document.getElementById('connect');
    const disconnectButton = document.getElementById('disconnect');
    let socket;
    let audioContext;
    const disconnect = () => {
        socket?.close();
        audioContext?.close();
        socket = null;
        audioContext = null;

        connectButton.disabled = false;
        disconnectButton.disabled = true;
        statusElement.innerText = "Disconnected";
    };

    disconnectButton.addEventListener('click', () => disconnect());
    connectButton.addEventListener('click', async () => {
        connectButton.disabled = true;

        // Ask for microphone access
        const sampleRate = 16000;
        const stream = await navigator.mediaDevices.getUserMedia({ audio: {
            channelCount: 1,
            sampleRate: sampleRate,
        }});

        // Set up a buffer between the audio worklet and the websocket for user voice
        const dataBuffer = bufferedArray(4800, (data) => {
            if (!socket || socket.readyState !== WebSocket.OPEN) {
                disconnect();
            } else {
                socket.send(JSON.stringify({ kind: "AudioData", audioData: { 
                    data: btoa(String.fromCharCode(...data)),
                    timestamp: new Date().toISOString(),
                    participant: { rawId: "Unknown" },
                    isSilent: false
                }}));
            }
        });

        // Load our audio context and audio worklet
        audioContext = new AudioContext({ sampleRate });
        await audioContext.audioWorklet.addModule("/audio.js");

        const voiceAgentNode = new AudioWorkletNode(audioContext, "VoiceAgentAudioWorklet");
        
        // Hook up speaker to the audio worklet (single channel splitter to up-mix to stereo)
        const splitter = audioContext.createChannelSplitter(1);
        voiceAgentNode.connect(splitter);
        splitter.connect(audioContext.destination);

        // Hook up microphone to the audio worklet
        voiceAgentNode.port.onmessage = (event) => dataBuffer.addData(event.data.buffer);
        const recordMediaStreamSource = audioContext.createMediaStreamSource(stream);
        recordMediaStreamSource.connect(voiceAgentNode);
        
        // Hook up to events from the websocket
        let connected = false;
        const receiveAudio = data => {
            if (data?.Kind == "StopAudio") {
                voiceAgentNode.port.postMessage(null);
            } else if (data?.Kind == "AudioData" && data?.AudioData?.Data) {
                if (!connected) {
                    connected = true;
                    statusElement.innerText = "Connected";
                }
                voiceAgentNode.port.postMessage(atobInt16(data.AudioData.Data));
            }
        }
        socket = new WebSocket("/api/audio");
        socket.onmessage = e => receiveAudio(JSON.parse(e.data));

        statusElement.innerText = "Connecting...";
        disconnectButton.disabled = false;
    });
    connectButton.disabled = false;
})();