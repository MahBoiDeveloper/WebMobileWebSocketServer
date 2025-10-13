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
        app.Map("/wss", async context =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
                return;
            
            var buffer = new byte[1024 * 4];
            using var _wss = await context.WebSockets.AcceptWebSocketAsync();
            try
            {
                while (_wss.State == WebSocketState.Open)
                {
                    var result = await _wss.ReceiveAsync(buffer, CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var con = connections.Where(c => c.Value.State == WebSocketState.Open).First();
                        await con.Value.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize<Data>(new Data(rng)))),
                                             WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("[Error] Unable to host /wss websocket server. Error message: " + ex.Message);
            }
        });

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
