using Azure.Communication.CallAutomation;
using Microsoft.CognitiveServices.Speech;

namespace VoiceAgent;

public record Config(
    Uri ACSEndpoint,
    string AISpeechResourceID,
    string AISpeechRegion,
    Uri AOAIEndpoint,
    string AOAIModelDeployment,
    string SpeechRecognitionLanguage,
    string SpeechSynthesisLanguage,
    string SpeechSynthesisVoiceName,
    string? Hostname
){
    public Uri BaseUri => new($"https://{GetHost()}");
    public Uri BaseWsUri => new($"wss://{GetHost()}");
    private string GetHost() {
        var websiteHostname = Environment.GetEnvironmentVariable("WEBSITE_DEFAULT_HOSTNAME");
        if (!string.IsNullOrWhiteSpace(websiteHostname))
        {
            return websiteHostname;
        }
        if (!string.IsNullOrWhiteSpace(Hostname))
        {
            return Hostname;
        }
        throw new InvalidOperationException("Hostname not set");
    }
    
    public GlobalAudioFormat GlobalAudioFormat = GlobalAudioFormat.Pcm16KMono16Bit;
    public AudioFormat ACSAudioFormat => GlobalAudioFormat switch
    {
        GlobalAudioFormat.Pcm16KMono16Bit => AudioFormat.Pcm16KMono,
        _ => throw new Exception($"Audio format {GlobalAudioFormat} not configured for ACS")
    };
    public SpeechSynthesisOutputFormat AISpeechAudioFormat => GlobalAudioFormat switch
    {
        GlobalAudioFormat.Pcm16KMono16Bit => SpeechSynthesisOutputFormat.Raw16Khz16BitMonoPcm,
        _ => throw new Exception($"Audio format {GlobalAudioFormat} not configured for AISpeech")
    };
}
public enum GlobalAudioFormat
{
    Pcm16KMono16Bit
}