using System.Net.WebSockets;
using System.Text;
using Azure.Communication.CallAutomation;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace VoiceAgent;

public sealed class VoiceConversation : IDisposable
{
    private readonly IAgentConversation _conversation;
    private readonly WebSocket _ws;
    private readonly SpeechSynthesizer _synthesizer;
    private readonly PushAudioInputStream _inputStream;
    private readonly SpeechRecognizer _recognizer;
    private readonly ILogger _logger;

    public VoiceConversation(IAgentConversation conversation, WebSocket ws, SpeechConfig ttsConfig, SpeechConfig sttConfig, ILogger logger)
    {
        _logger = logger;
        _conversation = conversation;
        _ws = ws;

        // Set up generation
        _synthesizer = new SpeechSynthesizer(ttsConfig, null);

        // Set up recognition
        _inputStream = new PushAudioInputStream();
        _recognizer = new SpeechRecognizer(sttConfig, AudioConfig.FromStreamInput(_inputStream));
        _recognizer.Canceled += (o, e) => _logger.LogWarning("Canceled: {Reason}: {ErrorDetails}", e.Reason, e.ErrorDetails);
        _recognizer.Recognizing += async (o, e) =>
        {
            await DetectInterruption(e.Result.Text);
        };
        _recognizer.Recognized += async (o, e) =>
        {
            await SpeechRecognized(e.Result.Text);
        };

        // Warm everything up
        // using (var connection = Connection.FromSpeechSynthesizer(_synthesizer)) connection.Open(true);
        // using (var connection = Connection.FromRecognizer(_recognizer)) connection.Open(true);
    }

    public async Task RunConversationAsync(CancellationToken cancellationToken)
    {
        // Send a welcome message - option 1, just sending a fixed message
        // await SendOneShotSpeechAsync("Hi there. How can I help you today?");

        // Option 2, trigger via the agent in the background
        _ = SpeechRecognized("Hi");

        // Start recognizing and start the receive loop
        await _recognizer.StartContinuousRecognitionAsync();
        _logger.LogInformation("Agent over voice is ready");
        try
        {
            await ReceiveAudioAsync(cancellationToken);
        }
        finally
        {
            // Clean up
            await _recognizer.StopContinuousRecognitionAsync();
        }
    }

    private async Task SendStopAudioAsync()
        => await SendJsonAsync(OutStreamingData.GetStopAudioForOutbound());

    private async Task SendAudioDataAsync(byte[] data)
        => await SendJsonAsync(OutStreamingData.GetAudioDataForOutbound(data));

    private async Task SendJsonAsync(string json)
    {
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
        await _ws.SendAsync(jsonBytes, WebSocketMessageType.Text, endOfMessage: true, default);
    }

    private async Task SendOneShotSpeechAsync(string text)
    {
        var audio = await _synthesizer.SpeakTextAsync(text);
        await SendAudioDataAsync(audio.AudioData);
    }

    private async Task DetectInterruption(string input)
    {
        int wordCountThreshold = 3;
        int wordCount = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        // This is a small example of an interrupt trigger, if we have 3 or more words said then we will interrupt the agent
        // If something is said that does not reach this threshold, we rely on the SpeechRecognized method to interrupt
        if (wordCount >= wordCountThreshold)
        {
            await SendStopAudioAsync();
        }
    }

    private async Task SpeechRecognized(string text)
    {
        // Skip over anything blank
        if (string.IsNullOrWhiteSpace(text)) return;

        // Pass the question to the agent and stream back the response
        using var request = new SpeechSynthesisRequest(SpeechSynthesisRequestInputType.TextStream);
        var ttsTask = await _synthesizer.StartSpeakingAsync(request);
        await Task.WhenAll(
            Task.Run(async () =>
            {
                bool isSilent = false;
                using var audioDataStream = AudioDataStream.FromResult(ttsTask);
                byte[] buffer = new byte[32000];
                while (true)
                {
                    var count = audioDataStream.ReadData(buffer);
                    if (count <= 0) break;

                    if (!isSilent)
                    {
                        await SendStopAudioAsync();
                        isSilent = true;
                    }

                    await SendAudioDataAsync(buffer[0..(int)count]);
                }
            }),
            Task.Run(async () =>
            {
                await foreach (var part in _conversation.GetResponseStreamAsync(text, default))
                {
                    request.InputStream.Write(part);
                }
                request.InputStream.Close();
            })
        );
    }

    private async Task ReceiveAudioAsync(CancellationToken cancellationToken)
    {
        ArraySegment<byte> buffer = new byte[10240];
        while (!cancellationToken.IsCancellationRequested && _ws.State == WebSocketState.Open)
        {
            // Receive audio from the WebSocket
            var result = await _ws.ReceiveAsync(buffer, cancellationToken);
            if (result.Count <= 0 || result.MessageType != WebSocketMessageType.Text) continue;

            // Parse it as media
            string data = Encoding.UTF8.GetString(buffer.Slice(0, result.Count));
            var input = StreamingData.Parse(data);
            if (input is AudioData audioData)
            {
                _inputStream.Write(audioData.Data);
            }
        }
    }

    public void Dispose()
    {
        _recognizer.Dispose();
        _inputStream.Dispose();
        _synthesizer.Dispose();
    }
}