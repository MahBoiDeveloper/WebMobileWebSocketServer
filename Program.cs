using Fleck;
namespace WebMobileWebSocketServer;

public class Program
{
    public static void Main(string[] args)
    {
        Random rng = new Random();
        WebSocketServer wss = new("ws://0.0.0.0:40000");
        wss.Start(con =>
        {
        con.OnMessage = msg => con.Send(msg switch
        {
            "get" => rng.NextInt64().ToString(),
            _ => "[wss] Message len: " + msg.Length
        });
            con.OnOpen = () => con.Send("[wss] Greetings!");
            con.OnClose = () => con.Send("[wss] Goodbye!");
        });

        WebApplication.CreateBuilder(args).Build().Run();
    }
}
