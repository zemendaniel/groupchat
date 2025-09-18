using System.Drawing;
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
    private const int port = 29999;
    private string name;
    private static UdpClient client;
    private CancellationTokenSource cts;
    private CancellationToken token;
    private IPAddress ip;
    private IPAddress mask;
    private IPAddress broadcast;
    private StringBuilder inputBuffer;
    private ReceiveDelegate receiveCallback;
    
    public Chat(ReceiveDelegate receiveCallback, string name)
    {
        this.receiveCallback = receiveCallback;
        this.name = name;
        
        client = new UdpClient(port);
        cts = new CancellationTokenSource();
        token = cts.Token;
        inputBuffer = new StringBuilder();
        
        (ip, mask, broadcast) = NetUtils.GetEthernetNetworkInfo();
        Console.WriteLine($"IP: {ip} | Mask: {mask} | Broadcast: {broadcast}");
        receiveCallback(FormatMessage("info", $"IP: {ip} | Mask: {mask} | Broadcast: {broadcast}"), MessageType.Info);
        
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
        await client.SendAsync(data, data.Length, new IPEndPoint(IPAddress.Broadcast, port));
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