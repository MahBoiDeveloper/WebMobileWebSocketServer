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
        X509Certificate2 certificate = new(certificatePath, privateKeyPath);
        WebSocketServer wss = new("wss://0.0.0.0:40000", certificate is not null);
        wss.Start(con =>
        {
        con.OnMessage = msg => 
        {
            string ret = msg switch
            {
                "get" => JsonSerializer.Serialize<Data>(new Data(rng)),
                _ => "[wss] Message len: " + msg.Length
            };
            con.Send(ret);
            Console.WriteLine("Sending on get request: " + ret);
        };
            con.OnOpen = () => con.Send("[wss] Greetings!");
            con.OnClose = () => con.Send("[wss] Goodbye!");
        });

        WebApplication.CreateBuilder(args).Build().Run();
    }
}
