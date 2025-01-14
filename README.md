# Simple ACS Voice Agent
A small code sample using ACS for a Voice Agent chat

## Prerequisites
 - An Azure Communication Services instance
 - An Azure Speech resource
 - An Azure OpenAI resource with GPT-4o deployed
 - NGROK or Microsoft DevTunnels for local development

## Getting Started
 - Clone the repository
 - Run `dotnet restore` to install dependencies
 - To run the backend service, run `dotnet run --project src/VoiceAgent`, this will start the backend service on port 5191
 - To use the developer console to interact with the voice agent, run `dotnet run --project src/DevConsole`, this will start the developer console and automatically connect to the backend service
 - To use ACS to interact with the voice agent, navigate to http://localhost:5191/

## Configuration Guide
Take a copy of `appsettings.json` and rename it to `appsettings.Development.json` for your development values, this is the file that will be used by the application when running in development mode, and is ignored by git.  

The following values are required:
 - `Hostname` - This should be an externally accessible hostname through NGROK or Microsoft DevTunnels. Only the hostname is required, no protocol or path. 
 - `ACSEndpoint` - The endpoint for the Azure Communication Services instance you are using. You should have the "Communication and Email Service Owner" role on this instance.
 - `AISpeechResourceID` - The full resource ID for the Azure Speech resource you are using. This should be in the format "/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.CognitiveServices/accounts/{resourceName}". You should have the "Cognitive Services Speech User" role on this resource.
 - `AISpeechRegion` - The region for the Azure Speech resource you are using.
 - `AOAIEndpoint` - The endpoint for the Azure OpenAI instance you are using. You should have the "Cognitive Services OpenAI User" role on this instance.
 - `AOAIModelDeployment` - The deployment name for the Azure OpenAI model you are using. It is recommended to use GPT-4o for accuracy and latency.
 - `SpeechRecognitionLanguage` - The language code for the speech recognition service. This should be a BCP-47 language tag. (Eg: "en-US")
 - `SpeechSynthesisLanguage` - The language code for the speech synthesis service. This should be a BCP-47 language tag. (Eg: "en-US")
 - `SpeechSynthesisVoiceName` - The Azure AI Speech voice name to use for voice synthesis. (Eg: "en-US-ChristopherMultilingualNeural")

 ## Features
  - [Agent](./src/VoiceAgent/Agent/Agent.cs) & [AgentConversation](./src/VoiceAgent/Agent/AgentConversation.cs) manage the textual conversation between the user and the agent, this can be stubbed out for testing, or pointed towards an external API,
  - [Voice](./src/VoiceAgent/Voice/Voice.cs) & [VoiceConversation](./src/VoiceAgent/Voice/VoiceConversation.cs) manage all of the text-to-speech and speech-to-text actions, wrapping the text agent in a voice interface.
    - [DetectInterruption()](./src/VoiceAgent/Voice/VoiceConversation.cs#L82) shows an example of how to detect when the user has interrupted the agent, and how to handle it.
  - [CallHandler](./src/VoiceAgent/Handlers/CallHandler.cs) manages the connection between the user and ACS, it is used to initiate the call and point it towards the websocket endpoint.
  - [DevConsole](./src/DevConsole/Program.cs) is a simple console application that can be used to interact with the voice agent using a local microphone and speaker, it connects to the backend in the same way that ACS would.