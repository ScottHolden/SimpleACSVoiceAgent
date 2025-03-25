using Azure.Messaging;

namespace ACSFrontend;

public class EventProcessor(
    IEnumerable<IEventGridHandler> _handlers,
    ILogger<EventProcessor> _logger
)
{
    public async Task<IResult> ProcessEventAsync(HttpRequest request)
    {
        try
        {
            var requestData = await BinaryData.FromStreamAsync(request.Body);
            var events = CloudEvent.ParseMany(requestData);
            var tasks = events?.SelectMany(x => GetHandlers(x.Type).Select(y => y.HandleEventAsync(x))).ToArray();
            _logger.LogInformation("Processing {Count} tasks for {EventCount} events", tasks?.Length, events?.Length);
            if (tasks == null || tasks.Length == 0)
            {
                return Results.BadRequest("No events found");
            }
            await Task.WhenAll(tasks);
            return Results.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event");
            return Results.InternalServerError(ex.Message);
        }
    }
    private IEnumerable<IEventGridHandler> GetHandlers(string eventType)
        => _handlers.Where(x => x.EventTypes.Contains(eventType, StringComparer.OrdinalIgnoreCase));
}