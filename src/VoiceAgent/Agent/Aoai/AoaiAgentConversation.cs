using System.Runtime.CompilerServices;
using System.Text;
using OpenAI.Chat;

namespace VoiceAgent;

public class AoaiAgentConversation(
    string _prompt,
    ChatClient _chatClient,
    ILogger _logger
) : IAgentConversation
{
    private readonly List<ChatMessage> _messages = [
        ChatMessage.CreateSystemMessage(_prompt)
    ];
    public async IAsyncEnumerable<string> GetResponseStreamAsync(string input, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        PerfTimer? t = new(_logger);
        PerfTimer t2 = new(_logger);

        _messages.Add(ChatMessage.CreateUserMessage(input));
        StringBuilder contentBuilder = new();
        t2.Start("CompleteChatStreamingAsync - WholeResponse");
        t.Start("CompleteChatStreamingAsync - FirstByte");
        await foreach (var resp in _chatClient.CompleteChatStreamingAsync(_messages, null, cancellationToken))
        {
            t?.Stop();
            var part = resp.ContentUpdate.FirstOrDefault()?.Text;
            if (string.IsNullOrEmpty(part)) continue;
            contentBuilder.Append(part);
            yield return part;
        }
        _messages.Add(ChatMessage.CreateAssistantMessage(contentBuilder.ToString()));
        t2.Stop();
    }
    public async Task WarmupAsync()
    {
        PerfTimer t = new(_logger);
        t.Start("Warmup");
        await _chatClient.CompleteChatAsync([
            ChatMessage.CreateSystemMessage("Hello"),
            ChatMessage.CreateUserMessage("Hello"),
        ], new ChatCompletionOptions
        {
            MaxOutputTokenCount = 10
        }, CancellationToken.None);
        t.Stop();
    }
}