using Azure.AI.OpenAI;
using Azure.Core;
using OpenAI.Chat;
using VoiceAgent;

var builder = WebApplication.CreateBuilder(args);

// Config
builder.Services.AddTransient(context 
    => context.GetRequiredService<IConfiguration>().Get<Config>()
        ?? throw new Exception("Unable to bind config")
);

// Internal services
builder.Services.AddSingleton<IAgent, AoaiAgent>();

builder.Services.AddSingleton<Voice>();
builder.Services.AddSingleton<WebsocketHandler>();

// Dependencies
builder.Services.AddAzureTokenCredential();
builder.Services.AddSingleton<ChatClient>(services => {
    var config = services.GetRequiredService<Config>();
    var credential = services.GetRequiredService<TokenCredential>();
    var aoaiClient = new AzureOpenAIClient(config.AOAIEndpoint, credential);
    return aoaiClient.GetChatClient(config.AOAIModelDeployment);
});

// Build
var app = builder.Build();
app.UseWebSockets();

// Map our websocket endpoint
app.Map("/api/audio", async (HttpContext context, WebsocketHandler handler)
    => await handler.Handle(context)
);

// Map a logging endpoint for optional use with ACS
app.Map("/api/events", async (HttpContext context, ILogger logger) => {
    using StreamReader bodyReader = new(context.Request.Body);
    var body = await bodyReader.ReadToEndAsync();
    logger.LogInformation("Event Log: {Log}", body);
    context.Response.StatusCode = StatusCodes.Status200OK;
});

// Warmup time!
_ = app.Services.GetRequiredService<Voice>().WarmupAsync();
_ = app.Services.GetService<AoaiAgent>()?.WarmupAsync();

app.Run();