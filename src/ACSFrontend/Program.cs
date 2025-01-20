using ACSFrontend;
using Azure;
using Azure.Communication.CallAutomation;
using Azure.Communication.Identity;
using Azure.Core;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Config
builder.Services.AddTransient(context 
    => context.GetRequiredService<IConfiguration>().Get<Config>()
        ?? throw new Exception("Unable to bind config")
);

// Internal services
builder.Services.AddSingleton<CallHandler>();

// Dependencies
builder.Services.AddAzureTokenCredential();
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
app.MapPost("/api/identity", async (CallHandler handler) 
    => await handler.GetIdentityAsync()
);
app.MapPost("/api/call", async ([FromBody] CallRequest request, CallHandler handler) =>
{
    await handler.MakeCallAsync(request.RawId);
    return Results.Accepted();
});

app.Run();

record CallRequest(string RawId);