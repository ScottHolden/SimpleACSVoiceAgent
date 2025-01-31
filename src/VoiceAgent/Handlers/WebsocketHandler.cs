using System.Net.WebSockets;
using Microsoft.AspNetCore.Connections;

namespace VoiceAgent;

public class WebsocketHandler(
    Voice _voice,
    ILogger<WebsocketHandler> _logger
)
{
    public async Task Handle(string threadId, HttpContext context)
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            _logger.LogInformation("WebSocket open");

            try
            {
                await _voice.StartConversation(threadId, webSocket, cancellationToken: context.RequestAborted);
            }
            catch (TaskCanceledException)
            {
                // Fall through to finally to gracefully close the connection
            }
            catch (OperationCanceledException)
            {
                // No need to handle this, it's most likely just the client forcefully closing the connection
            }
            catch (Exception e)
            {
                // Catch it if it is actually an error
                _logger.LogError(e, "Websocket error");
                if (webSocket.State != WebSocketState.Closed && webSocket.State != WebSocketState.Aborted)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
            }
            finally
            {

                if (webSocket.State != WebSocketState.Closed && webSocket.State != WebSocketState.Aborted)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
                _logger.LogInformation("WebSocket closed");
            }

        }
        else context.Response.StatusCode = StatusCodes.Status400BadRequest;
    }
}