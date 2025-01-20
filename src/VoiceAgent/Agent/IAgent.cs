namespace VoiceAgent;

public interface IAgent {
    Task<IAgentConversation> StartConversationAsync();
}