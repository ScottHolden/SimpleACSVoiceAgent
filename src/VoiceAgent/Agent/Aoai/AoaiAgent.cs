using OpenAI.Chat;

namespace VoiceAgent;

public class AoaiAgent(
    ChatClient _chatClient,
    ILogger<AoaiAgent> _logger
) : IAgent
{
    private readonly string _defaultPrompt = """
        You are a voice assistant, keep all responses to a single sentence that could be read aloud.
        Keep all responses to a single sentence that could be read aloud, do not use markdown, lists, or any other non-verbal formatting.
        """;

    public Task<IAgentConversation> StartConversationAsync(string threadId)
        => Task.FromResult((IAgentConversation)new AoaiAgentConversation(_defaultPrompt, _chatClient, _logger));
}
