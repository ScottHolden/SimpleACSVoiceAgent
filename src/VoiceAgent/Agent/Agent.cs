using OpenAI.Chat;

namespace VoiceAgent;

public class Agent(
    ChatClient _chatClient
)
{
    private readonly string _defaultPrompt = """
        You are a voice assistant, keep all responses to a single sentence that could be read aloud.
        Keep all responses to a single sentence that could be read aloud, do not use markdown, lists, or any other non-verbal formatting.
        """;

    public Task<AgentConversation> StartConversationAsync()
        => Task.FromResult(new AgentConversation(_defaultPrompt, _chatClient));

    public async Task WarmupAsync() => await new AgentConversation(_defaultPrompt, _chatClient).WarmupAsync();
}
