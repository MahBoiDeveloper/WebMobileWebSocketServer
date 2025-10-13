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
        string certificatePath = "/etc/letsencrypt/live/expserver.site/fullchain.pem";
        string privateKeyPath = "/etc/letsencrypt/live/expserver.site/privkey.pem";
        var connections = new ConcurrentDictionary<Guid, WebSocket>();

        var app = WebApplication.CreateBuilder(args).Build();

        app.UseWebSockets();
        // https://websocket.org/guides/languages/csharp/#aspnet-core-websocket-server
        //app.Map("/wss", async context =>
        //{
        //    if (!context.WebSockets.IsWebSocketRequest)
        //        return;

        //    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        //    var handler = context.RequestServices.GetRequiredService<WebSocketHandler>();
        //    await handler.HandleAsync(context, webSocket);
        //});

        try
        {
            if (!new FileInfo(certificatePath).Exists)
            {
                Console.WriteLine($"[Warning] File {certificatePath} doesn't found!");
                throw new("Unable to find open key.");
            }

            if (!new FileInfo(privateKeyPath).Exists)
            {
                Console.WriteLine($"[Warning] File {privateKeyPath} doesn't found!");
                throw new("Unable to find private key.");
            }

            X509Certificate2 cert = new(certificatePath, privateKeyPath);

            if (!cert.Verify())
                Console.WriteLine($"[Warning] Certificate isn't verified!");

            WebSocketServer wss = new("wss://0.0.0.0:41000")
            {
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12,
                Certificate = cert
            };
            wss.Start(wsConfig());
        }
        catch(Exception ex)
        {
            Console.WriteLine("[Error] Unable host secure websocket server connection for wss://0.0.0.0:41000. Error message: " + ex.Message);
        }

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
                Console.WriteLine("Sending on get request: " + ret);
            };
            con.OnOpen = () => con.Send($"[WebSocketServer] Greetings!");
            con.OnClose = () => con.Send($"[WebSocketServer] Goodbye!");
        };
    }
}
