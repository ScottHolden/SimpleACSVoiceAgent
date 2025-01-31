using System.Runtime.CompilerServices;
using VoiceAgent;

public class ExternalAgent : IAgent
{
    public Task<IAgentConversation> StartConversationAsync(string threadId)
        => Task.FromResult((IAgentConversation)new ExternalAgentConversation(threadId));
}

public class ExternalAgentConversation(
    string threadId
) : IAgentConversation
{
    private static readonly HttpClient _client = new();
    public async IAsyncEnumerable<string> GetResponseStreamAsync(string input, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var res = await _client.PostAsync("http://localhost:3000/api/agent", new StringContent(input), cancellationToken);
        yield return await res.Content.ReadAsStringAsync();
    }
    public Task WarmupAsync() => Task.CompletedTask;
}