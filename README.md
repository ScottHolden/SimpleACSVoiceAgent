# Simple Voice Agent
A small code sample showing how to run a Voice Agent

[![Deploy To Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FScottHolden%2FSimpleACSVoiceAgent%2Fmain%2Fdeploy%2Fdeploy.generated.json)

## Prerequisites
 - An Azure Speech resource
 - An Azure OpenAI resource with GPT-4o deployed (**required if using the example AoaiAgent**)
 - An Azure Communication Services instance (**required if using ACS Frontend**)
 - NGROK or Microsoft DevTunnels for local development (**required if using ACS Frontend**)

## Getting Started
 - Clone the repository
 - Run `dotnet restore` to install dependencies
 - To run the backend service, run `dotnet run --project src/VoiceAgent`, this will start the backend service on port 5191
 - To use the developer console to interact with the voice agent, run `dotnet run --project src/DevConsole`, this will start the developer console and automatically connect to the backend service
 - To use ACS to interact with the voice agent, configure ngrok to point to port 5191 using `ngrok http 5191` and update the hostname within the ACSFrontend appsettings, run `dotnet run --project src/DevConsole`, navigate to http://localhost:5058/

## Project Explanation
### VoiceAgent
This is a sample backend service that can be used to interact with a voice agent, it is designed to be used with a frontend service that can handle the voice connection, such as Azure Communication Services. The service is designed to be stateless and can be scaled horizontally to handle multiple conversations at once. It accepts audio via WebSockets. The service uses Azure AI Speech for speech recognition and synthesis, and has an example agent using Azure OpenAI for conversation. An external agent placeholder is provided if you already have a text agent you would like to use (some code changes required).

### DevConsole
This is a simple console application that can be used to interact with the voice agent using a local microphone and speaker, it connects to the backend in the same way that ACS would. This is useful for testing the voice agent without needing to set up a full frontend service.

### ACSFrontend
This is a sample frontend service that can be used to interact with the voice agent using Azure Communication Services. The service uses Azure Communication Services to initiate a call, connecting the user via a web browser to the VoiceAgent. When running locally ACS needs to be able to connect to the websocket, you can use NGROK or Microsoft DevTunnels to enable local development.

## Configuration Guide
Take a copy of `appsettings.json` and rename it to `appsettings.Development.json` for your development values, this is the file that will be used by the application when running in development mode, and is ignored by git.  

The following values are required:
**VoiceAgent**
 - `AISpeechResourceID` - The full resource ID for the Azure Speech resource you are using. This should be in the format "/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.CognitiveServices/accounts/{resourceName}". You should have the "Cognitive Services Speech User" role on this resource if you are not using key authentication.
 - `AISpeechRegion` - The region for the Azure Speech resource you are using.
 - `AISpeechKey` - *Only required when authenticating using a key, when left blank managed identity will be used instead.* The key for the Azure Speech resource you are using.

 - `AOAIEndpoint` - The endpoint for the Azure OpenAI instance you are using. You should have the "Cognitive Services OpenAI User" role on this instance.
 - `AOAIModelDeployment` - The deployment name for the Azure OpenAI model you are using. It is recommended to use GPT-4o for accuracy and latency.
 - `AOAIKey` - *Only required when authenticating using a key, when left blank managed identity will be used instead.* The key for the Azure OpenAI instance you are using.

 - `SpeechRecognitionLanguage` - The language code for the speech recognition service. This should be a BCP-47 language tag. (Eg: "en-US")
 - `SpeechSynthesisLanguage` - The language code for the speech synthesis service. This should be a BCP-47 language tag. (Eg: "en-US")
 - `SpeechSynthesisVoiceName` - The Azure AI Speech voice name to use for voice synthesis. (Eg: "en-US-ChristopherMultilingualNeural")

 **ACSFrontend**
 - `WebsocketHostname` - This should be an externally accessible hostname through NGROK or Microsoft DevTunnels. Only the hostname is required, no protocol or path. 
 - `ACSEndpoint` - The endpoint for the Azure Communication Services instance you are using. You should have the "Communication and Email Service Owner" role on this instance if you are not using key authentication.
 - `ACSKey` - *Only required when authenticating using a key, when left blank managed identity will be used instead.* The key for the Azure Communication Services instance you are using.

 ## Features
  - [Agent](./src/VoiceAgent/Agent/Agent.cs) & [AgentConversation](./src/VoiceAgent/Agent/AgentConversation.cs) manage the textual conversation between the user and the agent, this can be stubbed out for testing, or pointed towards an external API,
  - [Voice](./src/VoiceAgent/Voice/Voice.cs) & [VoiceConversation](./src/VoiceAgent/Voice/VoiceConversation.cs) manage all of the text-to-speech and speech-to-text actions, wrapping the text agent in a voice interface.
    - [DetectInterruption()](./src/VoiceAgent/Voice/VoiceConversation.cs#L82) shows an example of how to detect when the user has interrupted the agent, and how to handle it.
  - [CallHandler](./src/ACSFrontend/Handlers/CallHandler.cs) manages the connection between the user and ACS, it is used to initiate the call and point it towards the websocket endpoint.
  - [DevConsole](./src/DevConsole/Program.cs) is a simple console application that can be used to interact with the voice agent using a local microphone and speaker, it connects to the backend in the same way that ACS would.

## ACS Frontend - Rebuilding the Frontend
If you make changes to the wwwroot folder in ACSFrontend you may need to recompile the javascript used for ACS calling. To do this:
 - Install NodeJS
 - cd into the `wwwroot/js` folder
 - Run `npm install`
 - Run `npx webpack build`
This should compile any changes made within `application.js` and bundle the ACS call client into `app.compiled.js`.
