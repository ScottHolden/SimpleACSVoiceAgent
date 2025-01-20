using ACSFrontend;
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