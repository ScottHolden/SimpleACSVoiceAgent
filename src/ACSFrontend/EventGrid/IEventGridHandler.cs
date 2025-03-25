using Azure.Messaging;

namespace ACSFrontend;

public interface IEventGridHandler
{
    string[] EventTypes { get; }
    Task HandleEventAsync(CloudEvent cloudEvent);
}
