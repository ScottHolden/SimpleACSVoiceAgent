using ACSFrontend;
using Azure;
using Azure.Communication.CallAutomation;
using Azure.Communication.Identity;
using Azure.Core;
using Azure.ResourceManager;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Config
builder.Services.AddTransient(context 
    => context.GetRequiredService<IConfiguration>().Get<Config>()
        ?? throw new Exception("Unable to bind config")
);

// Internal services
builder.Services.AddSingleton<CallHandler>();
builder.Services.AddSingleton<EventProcessor>();
builder.Services.AddSingleton<InboundCallHandler>();
builder.Services.AddSingleton<EventGridSubscriptionManager>();
builder.Services.AddSingleton<IEnumerable<IEventGridHandler>>(services => [
    services.GetRequiredService<InboundCallHandler>()
]);

// Dependencies
builder.Services.AddAzureTokenCredential();
builder.Services.AddSingleton<ArmClient>(services => new ArmClient(services.GetRequiredService<TokenCredential>()));
builder.Services.AddSingleton<CallAutomationClient>(services =>
{
    var logger = services.GetRequiredService<ILogger<CallAutomationClient>>();
    var config = services.GetRequiredService<Config>();
    if (string.IsNullOrWhiteSpace(config.ACSKey))
    {
        var credential = services.GetRequiredService<TokenCredential>();

        logger.LogInformation("Using managed identity for CallAutomationClient");

        return new CallAutomationClient(config.ACSEndpoint, credential);
    }
    else
    {
        logger.LogInformation("Using key auth for CallAutomationClient");

        return new CallAutomationClient($"endpoint={config.ACSEndpoint};accesskey={config.ACSKey}");
    }
});
builder.Services.AddSingleton<CommunicationIdentityClient>(services =>
{
    var logger = services.GetRequiredService<ILogger<CommunicationIdentityClient>>();
    var config = services.GetRequiredService<Config>();
    if (string.IsNullOrWhiteSpace(config.ACSKey))
    {
        var credential = services.GetRequiredService<TokenCredential>();

        logger.LogInformation("Using managed identity for CallAutomationClient");

        return new CommunicationIdentityClient(config.ACSEndpoint, credential);
    }
    else
    {
        logger.LogInformation("Using key auth for CallAutomationClient");

        return new CommunicationIdentityClient(config.ACSEndpoint, new AzureKeyCredential(config.ACSKey));
    }
});

// Build
var app = builder.Build();
app.UseWebSockets();
app.UseDefaultFiles();
app.UseStaticFiles(); // Only used to present a sample frontend for end-to-end ACS

// Map our APIs
app.MapPost("/api/identity", async ([FromServices] CallHandler handler) 
    => await handler.GetIdentityAsync()
);
app.MapPost("/api/call", async ([FromBody] CallRequest request, [FromServices] CallHandler handler) =>
{
    await handler.MakeCallAsync(request.RawId);
    return Results.Accepted();
});

// Map event grid endpoints
app.Map("/api/events", async (HttpContext context, [FromServices] EventProcessor processor) =>
{
    if (context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
    {
        // Allow Event Grid to call us, this is a cut-down version for development
        var webhookRequest = WebHookRequest.FromHeaders(context.Request);
        var webhookResponse = webhookRequest.ToResponse() with
        {
            WebHookAllowedRate = "*"
        };
        webhookResponse.AppendToHeaders(context.Response);
        return Results.Ok();
    }
    else if (context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
    {
        return await processor.ProcessEventAsync(context.Request);
    }
    return Results.BadRequest("Only POST requests are accepted");
});

// Map a logging endpoint for us to dump unmapped events
app.Map("/api/log", async (HttpContext context, [FromServices]ILogger<Program> logger) =>
{
    using StreamReader bodyReader = new(context.Request.Body);
    var body = await bodyReader.ReadToEndAsync();
    logger.LogInformation("Event Log: {Log}", body);
    context.Response.StatusCode = StatusCodes.Status200OK;
});

_ = app.TryAutoConfigureEventGridSubscriptionAsync();

app.Run();

record CallRequest(string RawId);