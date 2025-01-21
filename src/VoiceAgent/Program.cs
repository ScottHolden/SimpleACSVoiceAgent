using Azure;
using Azure.AI.OpenAI;
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

// Example agent using AOAI:
builder.Services.AddSingleton<IAgent, AoaiAgent>();

// or, Example agent that would call a backend:
// builder.Services.AddSingleton<IAgent, ExternalAgent>();

builder.Services.AddSingleton<Voice>();
builder.Services.AddSingleton<WebsocketHandler>();

// Dependencies
builder.Services.AddAzureTokenCredential();
builder.Services.AddSingleton<ChatClient>(services =>
{
    var logger = services.GetRequiredService<ILogger<ChatClient>>();
    var config = services.GetRequiredService<Config>();
    if (string.IsNullOrWhiteSpace(config.AOAIKey))
    {
        logger.LogInformation("Using managed identity for ChatClient");

        var credential = services.GetRequiredService<TokenCredential>();

        return new AzureOpenAIClient(config.AOAIEndpoint, credential)
                    .GetChatClient(config.AOAIModelDeployment);
    }
    else
    {
        logger.LogInformation("Using key auth for ChatClient");

        return new AzureOpenAIClient(config.AOAIEndpoint, new AzureKeyCredential(config.AOAIKey))
                    .GetChatClient(config.AOAIModelDeployment);
    }
});

// Build
var app = builder.Build();
app.UseWebSockets();
app.UseDefaultFiles();
app.UseStaticFiles(); // Only used for the dev web frontend

// Map our websocket endpoint
app.Map("/api/audio", async (HttpContext context, WebsocketHandler handler)
    => await handler.Handle(context)
);

// Map a logging endpoint for optional use with ACS
app.Map("/api/events", async (HttpContext context, [FromServices]ILogger<Program> logger) =>
{
    using StreamReader bodyReader = new(context.Request.Body);
    var body = await bodyReader.ReadToEndAsync();
    logger.LogInformation("Event Log: {Log}", body);
    context.Response.StatusCode = StatusCodes.Status200OK;
});

// Warmup time!
_ = app.Services.GetRequiredService<Voice>().WarmupAsync();
_ = app.Services.GetService<IAgent>()?.WarmupAsync();

app.Run();