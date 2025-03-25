using Azure.Communication.CallAutomation;

namespace ACSFrontend;

public record Config(
    string? WebsocketHostname,
    Uri ACSEndpoint,
    string? ACSKey = null,
    string? EventGridTopicResourceID = null,
    string? EventHostname = null
){
    public Uri BaseEventsUri => new($"https://{EventHostname}");
    public Uri BaseWsUri => new($"wss://{WebsocketHostname}");
    public GlobalAudioFormat GlobalAudioFormat = GlobalAudioFormat.Pcm16KMono16Bit;
    public AudioFormat ACSAudioFormat => GlobalAudioFormat switch
    {
        GlobalAudioFormat.Pcm16KMono16Bit => AudioFormat.Pcm16KMono,
        _ => throw new Exception($"Audio format {GlobalAudioFormat} not configured for ACS")
    };
}
public enum GlobalAudioFormat
{
    Pcm16KMono16Bit
}