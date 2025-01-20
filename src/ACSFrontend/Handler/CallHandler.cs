using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Communication.Identity;

namespace ACSFrontend;

public record IdentityResponse(string AccessToken, string UserRawId);

public class CallHandler(
    CallAutomationClient _callClient,
    CommunicationIdentityClient _identityClient,
    Config _config,
    ILogger<CallHandler> _logger
)
{
    public async Task<IdentityResponse> GetIdentityAsync()
    {
        var token = await _identityClient.CreateUserAndTokenAsync([CommunicationTokenScope.VoIPJoin]);
        return new IdentityResponse(token.Value.AccessToken.Token, token.Value.User.RawId);
    }
    public async Task MakeCallAsync(string rawId)
    {
        var callbackEndpoint = new Uri(_config.BaseUri, "/api/events");
        var websocketEndpoint = new Uri(_config.BaseWsUri, "/api/audio");

        var userId = CommunicationIdentifier.FromRawId(rawId) as CommunicationUserIdentifier ?? throw new InvalidOperationException("Invalid user ID");
        
        CallInvite ci = new (userId);

        _logger.LogInformation("Making call to {UserObjectId}", rawId);
        try
        {
            await _callClient.CreateCallAsync(new CreateCallOptions(ci, callbackEndpoint){
                MediaStreamingOptions = new MediaStreamingOptions(
                            websocketEndpoint,
                            MediaStreamingContent.Audio,
                            MediaStreamingAudioChannel.Mixed,
                            MediaStreamingTransport.Websocket,
                            true
                        )
                {
                    EnableBidirectional = true,
                    AudioFormat = _config.ACSAudioFormat
                }
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error making call to {UserObjectId}", rawId);
            throw;
        }
    }
}