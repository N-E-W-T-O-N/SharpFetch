using System.Buffers.Binary;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Numerics;
using System.Text.RegularExpressions;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Platforms.Common;

public sealed partial class NetworkProbe : INetworkProbe
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
            int? ipv4Prefix = null;
            string? ipv6 = null;
            int? ipv6Prefix = null;

            foreach (var addr in ipProps.UnicastAddresses)
            {
                if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    // Prefer non-APIPA (169.254.x.x) address
                    string ipStr = addr.Address.ToString();
                    if (ipv4 == null || !ipStr.StartsWith("169.254."))
                    {
                        ipv4 = ipStr;
                        ipv4Prefix = GetPrefixLength(addr);
                    }
                }
                else if (addr.Address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    // Prefer non-link-local (fe80::) IPv6
                    string ipStr = addr.Address.ToString();
                    if (ipv6 == null || !addr.Address.IsIPv6LinkLocal)
                    {
                        ipv6 = ipStr;
                        ipv6Prefix = addr.PrefixLength > 0 && addr.PrefixLength <= 128 ? addr.PrefixLength : null;
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
            bool isVirtual = IsVirtualInterface(ni.Name, ni.Description, type);

            results.Add(new NetworkInterfaceInfo
            {
                Name = ni.Name,
                Description = ni.Description,
                Type = type,
                Ipv4 = ipv4,
                Ipv4PrefixLength = ipv4Prefix,
                Ipv6 = ipv6,
                Ipv6PrefixLength = ipv6Prefix,
                MacAddress = mac,
                SpeedBitsPerSecond = ni.Speed > 0 ? ni.Speed : 0,
                IsUp = true,
                IsDefaultGateway = hasGateway,
                IsVirtual = isVirtual
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

    [GeneratedRegex(@"\b(cellular|lte|4g|5g|wwan)\b|mobile\s*broadband", RegexOptions.IgnoreCase)]
    private static partial Regex CellularRegex();

    [GeneratedRegex(@"\b(modem|dial-?up)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ModemRegex();

    internal static NetworkType MapInterfaceType(NetworkInterfaceType type, string description)
    {
        string descLower = description.ToLowerInvariant();

        // 1. Explicit Tunnel / VPN adapters
        if (descLower.Contains("wireguard") || descLower.Contains("openvpn") || descLower.Contains("tailscale") ||
            descLower.Contains("tap-") || descLower.Contains("tun-") || type == NetworkInterfaceType.Tunnel)
        {
            return NetworkType.Tunnel;
        }

        // 2. Cellular / Mobile Broadband (e.g. Sierra Wireless LTE, Qualcomm Snapdragon 5G, Fibocom WWAN)
        // Checked before generic "wireless" because many cellular modems have "Sierra Wireless" or "Wireless WAN" in their name.
        if (type is NetworkInterfaceType.Wwanpp or NetworkInterfaceType.Wwanpp2 ||
            CellularRegex().IsMatch(description))
        {
            return NetworkType.Cellular;
        }

        // 3. Dial-up / Modem
        if (type is NetworkInterfaceType.Ppp or NetworkInterfaceType.Slip or NetworkInterfaceType.Isdn ||
            ModemRegex().IsMatch(description))
        {
            return NetworkType.Modem;
        }

        // 4. Wi-Fi
        if (type == NetworkInterfaceType.Wireless80211 ||
            descLower.Contains("wi-fi") || descLower.Contains("wireless") || descLower.Contains("802.11") || descLower.Contains("wlan"))
        {
            return NetworkType.Wifi;
        }

        // 5. Ethernet
        if (type is NetworkInterfaceType.Ethernet or NetworkInterfaceType.GigabitEthernet or
                    NetworkInterfaceType.FastEthernetFx or NetworkInterfaceType.FastEthernetT ||
            descLower.Contains("ethernet") || descLower.Contains("gbe") || descLower.Contains("lan"))
        {
            return NetworkType.Ethernet;
        }

        return NetworkType.Other;
    }

    internal static bool IsVirtualInterface(string name, string description, NetworkType type)
    {
        if (type == NetworkType.Tunnel)
            return true;

        string nameLower = name.ToLowerInvariant();
        string descLower = description.ToLowerInvariant();

        if (nameLower.StartsWith("vethernet") ||
            nameLower.StartsWith("veth") ||
            nameLower.StartsWith("docker") ||
            nameLower.StartsWith("virbr") ||
            nameLower.StartsWith("lxcbr") ||
            nameLower.StartsWith("br-") ||
            nameLower.Contains("vmnet") ||
            nameLower.Contains("virtualbox") ||
            nameLower.Contains("tailscale") ||
            nameLower.Contains("wireguard") ||
            nameLower.Contains("wintun") ||
            // WireGuard/ZeroTier interfaces are conventionally named as a
            // "wg"/"zt" prefix followed by a digit or separator (wg0, zt-abc);
            // a bare Contains would false-positive on any adapter name that
            // happens to contain those two letters.
            nameLower.StartsWith("wg") ||
            nameLower.StartsWith("zt"))
        {
            return true;
        }

        if (descLower.Contains("virtual ethernet adapter") ||
            descLower.Contains("hyper-v") ||
            descLower.Contains("vmware") ||
            descLower.Contains("virtualbox") ||
            descLower.Contains("host-only") ||
            descLower.Contains("tap-") ||
            descLower.Contains("tun-") ||
            descLower.Contains("wintun") ||
            descLower.Contains("tailscale") ||
            descLower.Contains("wireguard") ||
            descLower.Contains("openvpn"))
        {
            return true;
        }

        return false;
    }

    private static int? GetPrefixLength(UnicastIPAddressInformation addr)
    {
        if (addr.PrefixLength is > 0 and <= 32)
        {
            return addr.PrefixLength;
        }

        if (addr.IPv4Mask != null)
        {
            byte[] maskBytes = addr.IPv4Mask.GetAddressBytes();
            if (maskBytes.Length == 4)
            {
                uint maskInt = BinaryPrimitives.ReadUInt32BigEndian(maskBytes);
                int count = BitOperations.PopCount(maskInt);
                if (count > 0)
                {
                    return count;
                }
            }
        }

        return null;
    }
}
