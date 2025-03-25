using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.EventGrid;
using Azure.ResourceManager.EventGrid.Models;

namespace ACSFrontend;

public class EventGridSubscriptionManager(
    Config _config,
    ArmClient _armClient,
    IEnumerable<IEventGridHandler> _handlers,
    ILogger<EventGridSubscriptionManager> _logger
)
{
    private readonly string _subscriptionName = "ACSTranslateSubscription";
    private EventGridSubscriptionData BuildEventGridSubscription()
    {
        var filter = new EventSubscriptionFilter();
        foreach (var eventType in _handlers.SelectMany(x => x.EventTypes).Distinct())
        {
            filter.IncludedEventTypes.Add(eventType);
        }

        return new EventGridSubscriptionData
        {
            EventDeliverySchema = EventDeliverySchema.CloudEventSchemaV1_0,
            Destination = new WebHookEventSubscriptionDestination
            {
                Endpoint = new Uri(_config.BaseEventsUri, "/api/events")
            },
            Filter = filter,
            RetryPolicy = new EventSubscriptionRetryPolicy
            {
                MaxDeliveryAttempts = 4,
                EventTimeToLiveInMinutes = 5
            }
        };
    }
    public async Task TryAutoConfigureAsync()
    {
        if (string.IsNullOrWhiteSpace(_config.EventGridTopicResourceID))
        {
            _logger.LogWarning("EventGrid Topic Resource ID is not configured. Skipping subscription auto configuration.");
            return;
        }
        try
        {
            _logger.LogInformation("Configuring EventGrid subscription...");
            var topicResource = _armClient.GetSystemTopicResource(new ResourceIdentifier(_config.EventGridTopicResourceID));
            var topic = await topicResource.GetAsync();
            var subscriptions = topic.Value.GetSystemTopicEventSubscriptions();
            var subscription = BuildEventGridSubscription();
            await subscriptions.CreateOrUpdateAsync(WaitUntil.Completed, _subscriptionName, subscription);
            _logger.LogInformation("EventGrid subscription configured to {Endpoint}", subscription.Destination);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure EventGrid subscription: {Message}", ex.Message);
            return;
        }
    }
}

public static class EventGridSubscriptionManagerExtensions
{
    public static async Task TryAutoConfigureEventGridSubscriptionAsync(this IHost app)
        => await app.Services.GetRequiredService<EventGridSubscriptionManager>().TryAutoConfigureAsync();
}