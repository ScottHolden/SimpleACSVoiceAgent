using Azure.Communication.CallAutomation;
using Azure.Messaging;

namespace ACSFrontend;

class InboundCallHandler(
    CallAutomationClient _callClient,
    Config _config,
    ILogger<InboundCallHandler> _logger
) : IEventGridHandler
{
    private static readonly TimeSpan _maxEventAge = TimeSpan.FromMinutes(3);
    public string[] EventTypes { get; } = [
        "Microsoft.Communication.IncomingCall"
    ];

    public async Task HandleEventAsync(CloudEvent cloudEvent)
    {
        _logger.LogInformation("Received incoming call event: {Event}", cloudEvent);
        if (cloudEvent.Time == null || cloudEvent.Time.Value.Add(_maxEventAge) < DateTimeOffset.UtcNow)
        {
            _logger.LogWarning("Event is too old, ignoring");
            return;
        }

        var incomingCallData = cloudEvent.Data?.ToObjectFromJson<IncomingCallData>();
        if (incomingCallData == null)
        {
            _logger.LogWarning("No data found in event");
            return;
        }

        // We only want to handle inbound calls from PSTN numbers in this demo
        if (!incomingCallData.from.rawId.StartsWith("4:"))
        {
            _logger.LogWarning("Inbound call did not match PSTN, ignoring");
            return;
        }

        var logEndpoint = new Uri(_config.BaseEventsUri, "/api/log");
        var websocketEndpoint = new Uri(_config.BaseWsUri, "/api/audio");

        await _callClient.AnswerCallAsync(new AnswerCallOptions(
            incomingCallData.incomingCallContext,
            logEndpoint
            ){
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
            }
        );
    }

    private record IncomingCallData(string incomingCallContext, CallPartyData to, CallPartyData from, CustomContextData customContext);
    private record CallPartyData(string kind, string rawId);
    private record CustomContextData(Dictionary<string, string> voipHeaders);
}