using System.Text.Json;
using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MediaKind
{
    AudioData,
    StopAudio
}
public record MediaData(MediaKind Kind, AudioData? AudioData)
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
    public static MediaData OutboundAudio(byte[] data)
        => new(MediaKind.AudioData, new AudioData(data, DateTimeOffset.UtcNow, Participant.Unknown, false));
    public static MediaData Parse(string json)
        => JsonSerializer.Deserialize<MediaData>(json, _options) ?? throw new JsonException();
    public string ToJson()
        => JsonSerializer.Serialize(this, _options);
}
public record AudioData(byte[] Data, DateTimeOffset Timestamp, Participant Participant, bool IsSilent);
public record Participant(string RawId){
    public static readonly Participant Unknown = new("Unknown");
}