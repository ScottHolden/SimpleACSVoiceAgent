using Azure.Communication.CallAutomation;

namespace ACSFrontend;

public record Config(
    string? WebsocketHostname,
    Uri ACSEndpoint,
    string? ACSKey = null
){
    public Uri BaseUri => new($"https://{GetHost()}");
    public Uri BaseWsUri => new($"wss://{GetHost()}");
    private string GetHost() {
        var websiteHostname = Environment.GetEnvironmentVariable("WEBSITE_DEFAULT_HOSTNAME");
        if (!string.IsNullOrWhiteSpace(websiteHostname))
        {
            return websiteHostname;
        }
        if (!string.IsNullOrWhiteSpace(WebsocketHostname))
        {
            return WebsocketHostname.Replace("https://", "", StringComparison.OrdinalIgnoreCase).Replace("http://", "", StringComparison.OrdinalIgnoreCase).Trim('/');
        }
        throw new InvalidOperationException("Hostname not set");
    }
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