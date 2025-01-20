using System.Runtime.CompilerServices;
using VoiceAgent;

public class ExternalAgent : IAgent
{
    public Task<IAgentConversation> StartConversationAsync()
        => Task.FromResult((IAgentConversation)new ExternalAgentConversation());
}

public class ExternalAgentConversation : IAgentConversation
{
    public async IAsyncEnumerable<string> GetResponseStreamAsync(string input, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Delay(10);
        yield return "Hello, I am an external agent";
    }
}