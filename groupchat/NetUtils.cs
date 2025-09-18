using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace groupchat;

public static class NetUtils
{
    public static (IPAddress ip, IPAddress mask, IPAddress broadcast) GetEthernetNetworkInfo()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.NetworkInterfaceType != NetworkInterfaceType.Ethernet ||
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
                return (ip, mask, broadcast);
            }
        }

        throw new Exception("Could not determine IP address.");
    }
}