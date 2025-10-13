using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace WebMobileWebSocketServer;

/// <summary>
/// https://websocket.org/guides/languages/csharp/#aspnet-core-websocket-server
/// </summary>
public class WebSocketConnectionManager
{
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();

    public string AddConnection(WebSocket webSocket)
    {
        var connectionId = Guid.NewGuid().ToString();
        _connections.TryAdd(connectionId, webSocket);
        return connectionId;
    }

    public void RemoveConnection(string connectionId)
    {
        _connections.TryRemove(connectionId, out _);
    }

    public WebSocket GetConnection(string connectionId)
    {
        _connections.TryGetValue(connectionId, out var connection);
        return connection;
    }

    public IEnumerable<string> GetAllConnectionIds()
    {
        return _connections.Keys;
    }

    public async Task SendAsync(string connectionId, string message)
    {
        if (_connections.TryGetValue(connectionId, out var webSocket))
        {
            if (webSocket.State == WebSocketState.Open)
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                await webSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
            }
        }
    }

    public async Task BroadcastAsync(string message, string excludeConnectionId = null)
    {
        var tasks = new List<Task>();

        foreach (var pair in _connections)
        {
            if (pair.Key != excludeConnectionId && pair.Value.State == WebSocketState.Open)
            {
                tasks.Add(SendAsync(pair.Key, message));
            }
        }

        await Task.WhenAll(tasks);
    }
}
