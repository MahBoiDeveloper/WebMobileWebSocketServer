using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace WebMobileWebSocketServer;

/// <summary>
/// https://websocket.org/guides/languages/csharp/#aspnet-core-websocket-server
/// </summary>
public class WebSocketHandler
{
    private readonly WebSocketConnectionManager _connectionManager;
    private readonly ILogger<WebSocketHandler> _logger;

    public WebSocketHandler(
        WebSocketConnectionManager connectionManager,
        ILogger<WebSocketHandler> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public async Task HandleAsync(HttpContext context, WebSocket webSocket)
    {
        var connectionId = _connectionManager.AddConnection(webSocket);
        _logger.LogInformation($"WebSocket connection established: {connectionId}");

        try
        {
            await ReceiveAsync(connectionId, webSocket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in WebSocket connection {connectionId}");
        }
        finally
        {
            _connectionManager.RemoveConnection(connectionId);
            _logger.LogInformation($"WebSocket connection closed: {connectionId}");
        }
    }

    private async Task ReceiveAsync(string connectionId, WebSocket webSocket)
    {
        var buffer = new ArraySegment<byte>(new byte[4096]);

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);

            switch (result.MessageType)
            {
                case WebSocketMessageType.Text:
                    var message = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                    await HandleTextMessage(connectionId, message);
                    break;

                case WebSocketMessageType.Binary:
                    await HandleBinaryMessage(connectionId, buffer.Array.Take(result.Count).ToArray());
                    break;

                case WebSocketMessageType.Close:
                    await webSocket.CloseAsync(
                        result.CloseStatus.Value,
                        result.CloseStatusDescription,
                        CancellationToken.None
                    );
                    break;
            }
        }
    }

    private async Task HandleTextMessage(string connectionId, string message)
    {
        _logger.LogInformation($"Received from {connectionId}: {message}");

        try
        {
            var json = JsonDocument.Parse(message);
            var type = json.RootElement.GetProperty("type").GetString();

            switch (type)
            {
                case "broadcast":
                    await BroadcastMessage(connectionId, message);
                    break;
                case "private":
                    await SendPrivateMessage(connectionId, json.RootElement);
                    break;
                default:
                    await Echo(connectionId, message);
                    break;
            }
        }
        catch (JsonException)
        {
            await Echo(connectionId, message);
        }
    }

    private async Task HandleBinaryMessage(string connectionId, byte[] data)
    {
        _logger.LogInformation($"Received binary from {connectionId}: {data.Length} bytes");
        // Process binary data
    }

    private async Task Echo(string connectionId, string message)
    {
        await _connectionManager.SendAsync(connectionId, $"Echo: {message}");
    }

    private async Task BroadcastMessage(string senderId, string message)
    {
        await _connectionManager.BroadcastAsync(message, senderId);
    }

    private async Task SendPrivateMessage(string senderId, JsonElement json)
    {
        var targetId = json.GetProperty("target").GetString();
        var content = json.GetProperty("content").GetString();

        await _connectionManager.SendAsync(targetId, content);
    }
}
