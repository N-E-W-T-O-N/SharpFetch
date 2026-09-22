using System.Net.NetworkInformation;
using System.Net.Sockets;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Platforms.Common;

public sealed class NetworkProbe : INetworkProbe
{
    public IReadOnlyList<NetworkInterfaceInfo> DetectInterfaces()
    {
        var results = new List<NetworkInterfaceInfo>();

        NetworkInterface[] interfaces;
        try
        {
            interfaces = NetworkInterface.GetAllNetworkInterfaces();
        }
        catch
        {
            return results;
        }

        foreach (var ni in interfaces)
        {
            // Skip loopback and interfaces that are completely down
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;

            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;

            var ipProps = ni.GetIPProperties();
            string? ipv4 = null;
            string? ipv6 = null;

            foreach (var addr in ipProps.UnicastAddresses)
            {
                if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    // Prefer non-APIPA (169.254.x.x) address
                    string ipStr = addr.Address.ToString();
                    if (ipv4 == null || !ipStr.StartsWith("169.254."))
                    {
                        ipv4 = ipStr;
                    }
                }
                else if (addr.Address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    // Prefer non-link-local (fe80::) IPv6
                    string ipStr = addr.Address.ToString();
                    if (ipv6 == null || !addr.Address.IsIPv6LinkLocal)
                    {
                        ipv6 = ipStr;
                    }
                }
            }

            // Skip interfaces with no assigned IP address
            if (string.IsNullOrEmpty(ipv4) && string.IsNullOrEmpty(ipv6))
                continue;

            // Determine if interface has a default gateway
            bool hasGateway = ipProps.GatewayAddresses.Any(g => g.Address != null && g.Address.ToString() != "0.0.0.0");

            // Format MAC address
            string? mac = null;
            byte[] macBytes = ni.GetPhysicalAddress().GetAddressBytes();
            if (macBytes.Length > 0)
            {
                mac = string.Join(":", macBytes.Select(b => b.ToString("X2")));
            }

            var type = MapInterfaceType(ni.NetworkInterfaceType, ni.Description);

            results.Add(new NetworkInterfaceInfo
            {
                Name = ni.Name,
                Description = ni.Description,
                Type = type,
                Ipv4 = ipv4,
                Ipv6 = ipv6,
                MacAddress = mac,
                SpeedBitsPerSecond = ni.Speed > 0 ? ni.Speed : 0,
                IsUp = true,
                IsDefaultGateway = hasGateway
            });
        }

        // Sort: Default Gateway first, then Ethernet/Wifi, then by Speed descending
        return results
            .OrderByDescending(r => r.IsDefaultGateway)
            .ThenBy(r => r.Type switch
            {
                NetworkType.Ethernet => 1,
                NetworkType.Wifi => 2,
                NetworkType.Cellular => 3,
                NetworkType.Modem => 4,
                NetworkType.Ppp => 5,
                NetworkType.Tunnel => 6,
                _ => 7
            })
            .ThenByDescending(r => r.SpeedBitsPerSecond)
            .ToList();
    }

    private static NetworkType MapInterfaceType(NetworkInterfaceType type, string description)
    {
        string descLower = description.ToLowerInvariant();

        if (descLower.Contains("wi-fi") || descLower.Contains("wireless") || descLower.Contains("802.11") || descLower.Contains("wlan"))
            return NetworkType.Wifi;

        if (descLower.Contains("cellular") || descLower.Contains("mobile") || descLower.Contains("lte") || descLower.Contains("5g") || descLower.Contains("wwan"))
            return NetworkType.Cellular;

        if (descLower.Contains("modem") || descLower.Contains("dial"))
            return NetworkType.Modem;

        if (descLower.Contains("wireguard") || descLower.Contains("openvpn") || descLower.Contains("tailscale") || descLower.Contains("tap-") || descLower.Contains("tun-"))
            return NetworkType.Tunnel;

        return type switch
        {
            NetworkInterfaceType.Ethernet or NetworkInterfaceType.GigabitEthernet or NetworkInterfaceType.FastEthernetFx or NetworkInterfaceType.FastEthernetT => NetworkType.Ethernet,
            NetworkInterfaceType.Wireless80211 => NetworkType.Wifi,
            NetworkInterfaceType.Wwanpp or NetworkInterfaceType.Wwanpp2 => NetworkType.Cellular,
            NetworkInterfaceType.Ppp or NetworkInterfaceType.Slip or NetworkInterfaceType.Isdn => NetworkType.Modem,
            NetworkInterfaceType.Tunnel => NetworkType.Tunnel,
            NetworkInterfaceType.Loopback => NetworkType.Loopback,
            _ => NetworkType.Other
        };
    }
}
