using Microsoft.CognitiveServices.Speech;

namespace VoiceAgent;

public record Config(
    string AISpeechResourceID,
    string AISpeechRegion,
    Uri AOAIEndpoint,
    string AOAIModelDeployment,
    string SpeechRecognitionLanguage,
    string SpeechSynthesisLanguage,
    string SpeechSynthesisVoiceName,
    string? AOAIKey = null,
    string? AISpeechKey = null
){
    public GlobalAudioFormat GlobalAudioFormat = GlobalAudioFormat.Pcm16KMono16Bit;
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