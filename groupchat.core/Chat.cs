using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace groupchat.core;

public enum MessageType
{
    Own,
    Remote,
    Info
}
public delegate void ReceiveDelegate(string message, MessageType type);

public class Chat
{
    private string name;
    private int port;
    private static UdpClient client;
    private CancellationTokenSource cts;
    private CancellationToken token;
    private IPAddress ip;
    private IPAddress broadcast;
    private ReceiveDelegate receiveCallback;

    public int Port
    {
        private init
        {
            if (value < 1 || value > 65535)
                throw new ArgumentOutOfRangeException(value.ToString());
            port = value;
        }
        get => port;
    }
    
    public Chat(ReceiveDelegate receiveCallback, string name, IPAddress broadcast, IPAddress ip, int port = 29999)
    {
        Port = port;
        this.receiveCallback = receiveCallback;
        this.name = name;
        client = new UdpClient(Port);
        cts = new CancellationTokenSource();
        token = cts.Token;
        this.broadcast = broadcast;
        this.ip = ip;
        
        _ = Task.Run(ReceiveAsync, token);
    }
    
    private static string FormatMessage(string sender, string message)
    {
        return $"[{sender.ToUpper()}]: {message}";
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
            
            receiveCallback(FormatMessage(msgObj.Sender, msgObj.Msg), MessageType.Remote);
            
        }
    }
    
    public async Task SendAsync(string msg)
    {
        if (msg.Length == 0)
            return;
        
        var json = JsonSerializer.Serialize(new Message { Sender = name, Msg = msg });
        var data = Encoding.UTF8.GetBytes(json);
        receiveCallback(FormatMessage(name, msg), MessageType.Own);
        Console.WriteLine();
        await client.SendAsync(data, data.Length, new IPEndPoint(broadcast, Port));
    }

    public async Task Dispose()
    {
        await cts.CancelAsync();
    }

}

internal class Message
{
    public required string Sender { get; init; }
    public required string Msg { get; init; }
}