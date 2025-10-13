using Fleck;
using System.Net.WebSockets;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using MSWS = System.Net.WebSockets;

namespace WebMobileWebSocketServer;

public class Program
{
    private static Random rng = new Random();

    public static void Main(string[] args)
    {
        var app = WebApplication.CreateBuilder(args).Build();

        //app.UseWebSockets();
        // https://websocket.org/guides/languages/csharp/#aspnet-core-websocket-server
        //app.Map("/wss", async context =>
        //{
        //    if (!context.WebSockets.IsWebSocketRequest)
        //        return;

        //    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        //    var handler = context.RequestServices.GetRequiredService<WebSocketHandler>();
        //    await handler.HandleAsync(context, webSocket);
        //});

        // Default server 40k
        WebSocketServer ws = new("ws://0.0.0.0:40000");
        ws.Start(wsConfig());
        app.Run();
    }

    private static Action<IWebSocketConnection> wsConfig()
    {
        return con =>
        {
            con.OnMessage = msg =>
            {
                string ret = msg switch
                {
                    "get" => JsonSerializer.Serialize<Data>(new Data(rng)),
                    _ => $"[WebSocketServer] Recieved unknown message: "
                };
                con.Send(ret);
                Console.WriteLine($"[{con.ConnectionInfo.ClientIpAddress}:{con.ConnectionInfo.ClientPort}] --> get ");
                Console.WriteLine(
                    $"[{con.ConnectionInfo.ClientIpAddress}:{con.ConnectionInfo.ClientPort}] <-- " 
                    + ret);
            };
            con.OnOpen = () => con.Send($"[WebSocketServer] Greetings!");
            con.OnClose = () => con.Send($"[WebSocketServer] Goodbye!");
        };
    }
}
