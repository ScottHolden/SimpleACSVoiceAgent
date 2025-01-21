namespace VoiceAgent;

public interface IAgent {
    Task<IAgentConversation> StartConversationAsync();
    async Task WarmupAsync() => await (await StartConversationAsync()).WarmupAsync();
}