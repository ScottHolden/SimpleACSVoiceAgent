import { CallClient } from "@azure/communication-calling";
import { AzureCommunicationTokenCredential } from '@azure/communication-common';

const callClient = new CallClient();
let call;
let callAgent;
let deviceManager;
let tokenCredential;
let rawId;

const statusElement = document.getElementById('status');
const connectButton = document.getElementById('connect');
const disconnectButton = document.getElementById('disconnect');

fetch('/api/identity', {
    method: 'POST',
})
    .then(res => res.json())
    .then(async userToken => {
        rawId = userToken.userRawId;
        tokenCredential = new AzureCommunicationTokenCredential(userToken.accessToken);
        callAgent = await callClient.createCallAgent(tokenCredential);
        deviceManager = await callClient.getDeviceManager();
        await deviceManager.askDevicePermission({ audio: true });
        callAgent.on('incomingCall', async (args) => {
            call = await args.incomingCall.accept();
            statusElement.innerText = 'Connected';
            disconnectButton.disabled = false;
        });
        statusElement.innerText = 'Ready';
        connectButton.disabled = false;
    });

connectButton.addEventListener('click', async () => {
    connectButton.disabled = true;
    fetch('/api/call', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ RawId: rawId }),
    })
        .then(() => {
            statusElement.innerText = 'Waiting for call...';
        })
});

disconnectButton.addEventListener('click', async () => {
    disconnectButton.disabled = true;
    connectButton.disabled = false;
    try {
        call.hangUp();
    }
    catch (e) {
        console.error(e);
    }
    statusElement.innerText = 'Ready';
});
