using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace groupchat.core;

public delegate void ReceiveDelegate(string message, Color color);
public delegate string SendDelegate();

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
    private SendDelegate sendCallback;
    
    public Chat(ReceiveDelegate receiveCallback, SendDelegate sendCallback, string name)
    {
        this.receiveCallback = receiveCallback;
        this.sendCallback = sendCallback;
        this.name = name;
        
        client = new UdpClient(port);
        cts = new CancellationTokenSource();
        token = cts.Token;
        inputBuffer = new StringBuilder();
        
        (ip, mask, broadcast) = NetUtils.GetEthernetNetworkInfo();
        receiveCallback(FormatMessage("info", $"IP: {ip} | Mask: {mask} | Broadcast: {broadcast}"), Color.DarkBlue);;
        
        _ = Task.Run(ReceiveAsync, token);
        _ = Task.Run(SendAsync, token);
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
            
            receiveCallback(FormatMessage(msgObj.Sender, msgObj.Msg), Color.DarkGreen);
            
        }
    }
    
    private async Task SendAsync()
    {
        while (!token.IsCancellationRequested)
        {
            var msg = sendCallback();
            
            if (msg.Length == 0)
                continue;
            
            var json = JsonSerializer.Serialize(new Message { Sender = name, Msg = msg });
            var data = Encoding.UTF8.GetBytes(json);
            receiveCallback(FormatMessage(name, msg), Color.DarkKhaki);
            await client.SendAsync(data, data.Length, new IPEndPoint(IPAddress.Broadcast, port));
            
        }
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