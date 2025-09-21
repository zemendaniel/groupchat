using System.Net;
using System.Net.Sockets;
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
    private readonly string name;
    private readonly int port;
    private readonly UdpClient client;
    private readonly CancellationTokenSource cts;
    private readonly CancellationToken token;
    private readonly IPAddress ip;
    private readonly IPAddress broadcast;
    private readonly ReceiveDelegate receiveCallback;
    private readonly Encryption encryption;

    private int Port
    {
        init
        {
            if (value is < 1 or > 65535)
                throw new ArgumentOutOfRangeException(value.ToString());
            port = value;
        }
        get => port;
    }
    
    public Chat(ReceiveDelegate receiveCallback, string name, IPAddress broadcast, IPAddress ip, int port = 29999, string password = "")
    {
        Port = port;
        this.receiveCallback = receiveCallback;
        this.name = name;
        client = new UdpClient(Port);
        encryption = new Encryption(password);
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
            UdpReceiveResult result;
            try
            {
                result = await client.ReceiveAsync(token);
            }
            catch (OperationCanceledException) { break; }
            catch { continue; }

            if (Equals(result.RemoteEndPoint.Address, ip))
                continue;

            string json;
            try
            {
                json = encryption.Decrypt(result.Buffer);
            }
            catch { continue; } // Ignore bad UTF-8 or wrong password
            
            Message? msgObj;
            try
            {
                msgObj = JsonSerializer.Deserialize<Message>(json);
            }
            catch { continue; } // Ignore bad JSON
            
            if (msgObj == null || string.IsNullOrEmpty(msgObj.Sender) || string.IsNullOrEmpty(msgObj.Msg))
                continue; // Ignore null or empty messages
            
            receiveCallback(FormatMessage(msgObj.Sender, msgObj.Msg), MessageType.Remote);
        }
    }
    
    public async Task SendAsync(string msg)
    {
        if (msg.Length == 0)
            return;
        
        var json = JsonSerializer.Serialize(new Message { Sender = name, Msg = msg });
        var data = encryption.Encrypt(json);
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