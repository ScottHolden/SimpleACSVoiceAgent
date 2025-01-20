using System.Net.WebSockets;
using Azure.Core;
using Microsoft.CognitiveServices.Speech;

namespace VoiceAgent;

public class Voice(
    IAgent _agent,
    Config _config,
    TokenCredential _tokenCredential,
    ILogger<Voice> _logger
)
{
    public async Task WarmupAsync() => await GetAuthTokenAsync(default);

    public async Task StartConversation(WebSocket ws, CancellationToken cancellationToken)
    {
        // Set up a conversation with our agent
        var conversation = await _agent.StartConversationAsync();

        // Set up speech defaults
        var sttConfig = await BuildSTTConfigAsync(cancellationToken);
        var ttsConfig = await BuildTTSConfigAsync(cancellationToken);

        using var vc = new VoiceConversation(conversation, ws, ttsConfig, sttConfig, _logger);
        await vc.RunConversationAsync(cancellationToken);
    }

    private async Task<SpeechConfig> BuildSTTConfigAsync(CancellationToken cancellationToken)
    {
        var region = _config.AISpeechRegion.Trim().ToLower().Replace(" ", "");
        SpeechConfig config;

        if (string.IsNullOrWhiteSpace(_config.AISpeechKey))
        {
            _logger.LogInformation("Using managed identity for STT");

            var authToken = await GetAuthTokenAsync(cancellationToken);
            config = SpeechConfig.FromAuthorizationToken(authToken, region);
        }
        else
        {
            _logger.LogInformation("Using key auth for STT");

            config = SpeechConfig.FromSubscription(_config.AISpeechKey, region);
        }

        // Configure semantic segmentation to reduce pauses
        config.SetProperty(PropertyId.Speech_SegmentationStrategy, "Semantic");

        // Configure the language
        config.SpeechRecognitionLanguage = _config.SpeechRecognitionLanguage;

        return config;
    }

    private async Task<SpeechConfig> BuildTTSConfigAsync(CancellationToken cancellationToken)
    {
        // Use the v2 endpoint for textstream support
        var region = _config.AISpeechRegion.Trim().ToLower().Replace(" ", "");
        var endpoint = new Uri($"wss://{region}.tts.speech.microsoft.com/cognitiveservices/websocket/v2");
        SpeechConfig config;

        if (string.IsNullOrWhiteSpace(_config.AISpeechKey))
        {
            _logger.LogInformation("Using managed identity for TTS");

            config = SpeechConfig.FromEndpoint(endpoint);
            config.AuthorizationToken = await GetAuthTokenAsync(cancellationToken);
        }
        else
        {
            _logger.LogInformation("Using key auth for TTS");

            config = SpeechConfig.FromEndpoint(endpoint, _config.AISpeechKey);
        }

        // Configure the voice
        config.SpeechSynthesisLanguage = _config.SpeechSynthesisLanguage;
        config.SpeechSynthesisVoiceName = _config.SpeechSynthesisVoiceName;
        config.SetSpeechSynthesisOutputFormat(_config.AISpeechAudioFormat);

        return config;
    }


    private readonly SemaphoreSlim _authLock = new(1);
    private string? _authToken;
    private DateTimeOffset _authTokenExpires = DateTimeOffset.MinValue;
    private async Task<string> GetAuthTokenAsync(CancellationToken cancellationToken)
    {
        if (_authToken != null && DateTimeOffset.Now < _authTokenExpires)
        {
            return _authToken;
        }
        await _authLock.WaitAsync(cancellationToken);
        try
        {
            if (_authToken != null && DateTimeOffset.Now < _authTokenExpires)
            {
                return _authToken;
            }

            _logger.LogInformation("Getting new cognitive services auth token");
            var entraAuth = await _tokenCredential.GetTokenAsync(new TokenRequestContext([
                "https://cognitiveservices.azure.com/.default"
            ]), cancellationToken);

            _authToken = $"aad#{_config.AISpeechResourceID}#{entraAuth.Token}";
            _authTokenExpires = entraAuth.ExpiresOn.AddMinutes(-5);
        }
        catch
        {
            _authToken = null;
        }
        finally
        {
            _authLock.Release();
        }

        return _authToken ?? throw new InvalidOperationException("Failed to get auth token");
    }
}