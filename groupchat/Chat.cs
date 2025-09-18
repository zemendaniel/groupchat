using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace groupchat;

public class Chat
{
    private const int port = 29999;
    private string name;
    private static UdpClient client;
    private CancellationTokenSource cts;
    private CancellationToken token;
    private IPAddress ip;
    private IPAddress mask;
    private IPAddress broadcast;

    public Chat()
    {
        Console.Write("Enter your name: ");
        while (true)
        {
            name = Console.ReadLine()!;
            if (name.Length < 1 || name.Length > 32)
                Console.WriteLine("Name must be between 1 and 32");
            else break;
        }
        client = new UdpClient(port);
        cts = new CancellationTokenSource();
        token = cts.Token;
        
        (ip, mask, broadcast) = NetUtils.GetEthernetNetworkInfo();
        
        _ = Task.Run(ReceiveAsync, token);
        _ = Task.Run(SendAsync, token);
    }
    
    private async Task ReceiveAsync()
    {
        while (!token.IsCancellationRequested)
        {
            var result = await client.ReceiveAsync(token);
            if (Equals(result.RemoteEndPoint.Address, ip))
                continue;
            
            var json = Encoding.UTF8.GetString(result.Buffer);
            var msgObj = JsonSerializer.Deserialize<Message>(json);
            
            if (msgObj == null)
                continue;
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[{msgObj.Sender}]: {msgObj.Msg}");
            Console.ResetColor();
            Console.Write(">>> "); 
        }
    }
    
    private async Task SendAsync()
    {
        while (!token.IsCancellationRequested)
        {
            Console.Write(">>> ");
            var msg = Console.ReadLine();
            
            if (msg!.Equals("/exit", StringComparison.CurrentCultureIgnoreCase)) break;

            var json = JsonSerializer.Serialize(new Message { Sender = name, Msg = msg });
            var data = Encoding.UTF8.GetBytes(json);
            await client.SendAsync(data, data.Length, new IPEndPoint(IPAddress.Broadcast, port));
        }
    }
}

internal class Message
{
    public required string Sender { get; init; }
    public required string Msg { get; init; }
}