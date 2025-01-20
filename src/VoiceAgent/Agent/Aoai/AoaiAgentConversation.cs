using System.Runtime.CompilerServices;
using System.Text;
using OpenAI.Chat;

namespace VoiceAgent;

public class AoaiAgentConversation(
    string _prompt,
    ChatClient _chatClient
) : IAgentConversation
{
    private readonly List<ChatMessage> _messages = [
        ChatMessage.CreateSystemMessage(_prompt)
    ];
    public async IAsyncEnumerable<string> GetResponseStreamAsync(string input, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _messages.Add(ChatMessage.CreateUserMessage(input));
        StringBuilder contentBuilder = new();
        await foreach (var resp in _chatClient.CompleteChatStreamingAsync(_messages, null, cancellationToken))
        {
            var part = resp.ContentUpdate.FirstOrDefault()?.Text;
            if (string.IsNullOrEmpty(part)) continue;
            contentBuilder.Append(part);
            yield return part;
        }
        _messages.Add(ChatMessage.CreateAssistantMessage(contentBuilder.ToString()));
    }
    public async Task WarmupAsync()
    {
        await _chatClient.CompleteChatAsync(_messages, new ChatCompletionOptions
        {
            MaxOutputTokenCount = 1
        }, CancellationToken.None);
    }
}