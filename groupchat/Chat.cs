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
    private StringBuilder inputBuffer;

    public Chat()
    {
        Console.Clear();
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
        inputBuffer = new StringBuilder();
        
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
            
            var currentLine = Console.CursorTop;
            Console.SetCursorPosition(0, currentLine);
            Console.Write(new string(' ', Console.WindowWidth)); 
            Console.SetCursorPosition(0, currentLine);
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[{msgObj.Sender}]: {msgObj.Msg}");
            Console.ResetColor();
            
            Console.Write(">>> " + inputBuffer);
        }
    }
    
    private async Task SendAsync()
    {
        Console.Clear();
        const string inputPromptString = ">>> ";
        while (!token.IsCancellationRequested)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(inputPromptString + inputBuffer);

            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Backspace)
            {
                if (inputBuffer.Length > 0)
                {
                    inputBuffer.Remove(inputBuffer.Length - 1, 1);
                    Console.SetCursorPosition(inputPromptString.Length, Console.CursorTop);
                    Console.Write(inputBuffer + " ");
                    Console.SetCursorPosition(inputPromptString.Length + inputBuffer.Length, Console.CursorTop);
                }
            }
            else if (key.Key == ConsoleKey.Enter)
            {
                var msg = inputBuffer.ToString();
                inputBuffer.Clear();
                Console.WriteLine();

                if (msg.Equals("/exit", StringComparison.CurrentCultureIgnoreCase))
                {
                    await cts.CancelAsync();
                    break;
                }

                if (msg.Length == 0)
                    continue;

                var json = JsonSerializer.Serialize(new Message { Sender = name, Msg = msg });
                var data = Encoding.UTF8.GetBytes(json);
                await client.SendAsync(data, data.Length, new IPEndPoint(IPAddress.Broadcast, port));
            }
            else
            {
                inputBuffer.Append(key.KeyChar);
                Console.SetCursorPosition(4, Console.CursorTop);
                Console.Write(inputBuffer);
            }
        }
    }

}

internal class Message
{
    public required string Sender { get; init; }
    public required string Msg { get; init; }
}