namespace VoiceAgent;

public interface IAgent {
    Task<IAgentConversation> StartConversationAsync(string threadId);
    async Task WarmupAsync() => await (await StartConversationAsync("")).WarmupAsync();
}