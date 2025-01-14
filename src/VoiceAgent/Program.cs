using Azure.AI.OpenAI;
using Azure.Communication.CallAutomation;
using Azure.Communication.Identity;
using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Chat;
using VoiceAgent;

var builder = WebApplication.CreateBuilder(args);

// Config
builder.Services.AddTransient(context 
    => context.GetRequiredService<IConfiguration>().Get<Config>()
        ?? throw new Exception("Unable to bind config")
);

// Internal services
builder.Services.AddSingleton<Agent>();
builder.Services.AddSingleton<Voice>();
builder.Services.AddSingleton<CallHandler>();
builder.Services.AddSingleton<WebsocketHandler>();

// Dependencies
builder.Services.AddAzureTokenCredential();
builder.Services.AddSingleton<CallAutomationClient>(services =>
{
    var endpoint = services.GetRequiredService<Config>().ACSEndpoint;
    var credential = services.GetRequiredService<TokenCredential>();
    return new CallAutomationClient(endpoint, credential);
});
builder.Services.AddSingleton<CommunicationIdentityClient>(services =>
{
    var endpoint = services.GetRequiredService<Config>().ACSEndpoint;
    var credential = services.GetRequiredService<TokenCredential>();
    return new CommunicationIdentityClient(endpoint, credential);
});
builder.Services.AddSingleton<ChatClient>(services => {
    var config = services.GetRequiredService<Config>();
    var credential = services.GetRequiredService<TokenCredential>();
    var aoaiClient = new AzureOpenAIClient(config.AOAIEndpoint, credential);
    return aoaiClient.GetChatClient(config.AOAIModelDeployment);
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
app.Map("/api/events", async (HttpContext context, CallHandler handler) 
    => await handler.LogAsync(context)
);

app.Map("/api/audio", async (HttpContext context, WebsocketHandler handler)
    => await handler.Handle(context)
);

// Warmup time!
await app.Services.GetRequiredService<Voice>().WarmupAsync();
await app.Services.GetRequiredService<Agent>().WarmupAsync();

app.Run();

record CallRequest(string RawId);