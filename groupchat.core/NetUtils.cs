using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace groupchat.core;

public class AdapterInfo
{
    public required string Name { get; init; } = "";
    public required string Description { get; init; } = "";
    public required IPAddress IP { get; init; }
    public required IPAddress Mask { get; init; }
    public required IPAddress Broadcast { get; init; }
    public required PhysicalAddress MAC { get; init; }
    public override string ToString() => $"{Name} ({Description}) - {IP}";
}

public static class NetUtils
{
    public static List<AdapterInfo> GetEthernetAdapters()
    {
        var list = new List<AdapterInfo>();

        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                ni.OperationalStatus != OperationalStatus.Up)
                continue;

            foreach (var info in ni.GetIPProperties().UnicastAddresses)
            {
                if (info.Address.AddressFamily != AddressFamily.InterNetwork) continue;

                var ip = info.Address;
                var mask = info.IPv4Mask;
                var ipBytes = ip.GetAddressBytes();
                var maskBytes = mask.GetAddressBytes();
                var broadcastBytes = new byte[4];
                for (var i = 0; i < 4; i++)
                    broadcastBytes[i] = (byte)(ipBytes[i] | (maskBytes[i] ^ 255));
                var broadcast = new IPAddress(broadcastBytes);

                list.Add(new AdapterInfo
                {
                    Name = ni.Name,
                    Description = ni.Description,
                    IP = ip,
                    Mask = mask,
                    Broadcast = broadcast,
                    MAC = ni.GetPhysicalAddress()
                });
            }
        }

        return list;
    }
}