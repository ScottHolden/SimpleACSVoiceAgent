namespace VoiceAgent;

public interface IAgentConversation {
    IAsyncEnumerable<string> GetResponseStreamAsync(string input, CancellationToken cancellationToken);
}