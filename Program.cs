using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Fleck;
namespace WebMobileWebSocketServer;

public class Program
{
    public static void Main(string[] args)
    {
        Random rng = new Random();
        string certificatePath = "/etc/letsencrypt/live/expserver.site/fullchain.pem";
        string privateKeyPath = "/etc/letsencrypt/live/expserver.site/privkey.pem";

        try
        {
            if (!new FileInfo(certificatePath).Exists)
            {
                Console.WriteLine($"[Warning] File {certificatePath} doens't found!");
            }

            if (!new FileInfo(privateKeyPath).Exists)
            {
                Console.WriteLine($"[Warning] File {privateKeyPath} doens't found!");
            }

            X509Certificate2 cert = new(certificatePath, privateKeyPath);

            if (!cert.Verify())
            {
                Console.WriteLine($"[Warning] Certificate isn't verified!");
            }
            WebSocketServer wss = new("wss://0.0.0.0:41000")
            {
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12,
                Certificate = cert
            };
            wss.Start(con =>
            {
                con.OnMessage = msg =>
                {
                    string ret = msg switch
                    {
                        "get" => JsonSerializer.Serialize<Data>(new Data(rng)),
                        _ => $"[{nameof(wss)}] Message len: " + msg.Length
                    };
                    con.Send(ret);
                    Console.WriteLine("Sending on get request: " + ret);
                };
                con.OnOpen = () => con.Send($"[{nameof(wss)}] Greetings!");
                con.OnClose = () => con.Send($"[{nameof(wss)}] Goodbye!");
            });
        }
        catch
        {
        }

        WebSocketServer ws = new("ws://0.0.0.0:40000");
        ws.Start(con =>
        {
            con.OnMessage = msg => 
            {
                string ret = msg switch
                {
                    "get" => JsonSerializer.Serialize<Data>(new Data(rng)),
                    _ => $"[{nameof(ws)}] Message len: " + msg.Length
                };
                con.Send(ret);
                Console.WriteLine("Sending on get request: " + ret);
            };
            con.OnOpen = () => con.Send($"[{nameof(ws)}] Greetings!");
            con.OnClose = () => con.Send($"[{nameof(ws)}] Goodbye!");
        });

        WebApplication.CreateBuilder(args).Build().Run();
    }
}
