namespace SharpFetch.Core.Models;

public enum NetworkType
{
    Ethernet,
    Wifi,
    Cellular,
    Modem,
    Ppp,
    Tunnel,
    Loopback,
    Other
}

public sealed record NetworkInterfaceInfo
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required NetworkType Type { get; init; }
    public string? Ipv4 { get; init; }
    public int? Ipv4PrefixLength { get; init; }
    public string? Ipv6 { get; init; }
    public int? Ipv6PrefixLength { get; init; }
    public string? MacAddress { get; init; }
    public long SpeedBitsPerSecond { get; init; }
    public bool IsUp { get; init; }
    public bool IsDefaultGateway { get; init; }
    public bool IsVirtual { get; init; }

    public string? Ipv4Cidr => Ipv4 != null && Ipv4PrefixLength.HasValue ? $"{Ipv4}/{Ipv4PrefixLength.Value}" : Ipv4;
    public string? Ipv6Cidr => Ipv6 != null && Ipv6PrefixLength.HasValue ? $"{Ipv6}/{Ipv6PrefixLength.Value}" : Ipv6;

    public string FormattedSpeed => SpeedBitsPerSecond switch
    {
        >= 1_000_000_000_000 => $"{SpeedBitsPerSecond / 1_000_000_000_000.0:0.#} Tbps",
        >= 1_000_000_000 => $"{SpeedBitsPerSecond / 1_000_000_000.0:0.#} Gbps",
        >= 1_000_000 => $"{SpeedBitsPerSecond / 1_000_000.0:0.#} Mbps",
        >= 1_000 => $"{SpeedBitsPerSecond / 1_000.0:0.#} Kbps",
        > 0 => $"{SpeedBitsPerSecond} bps",
        _ => "Unknown speed"
    };
}

public sealed record WifiConnectionInfo
{
    public required string InterfaceName { get; init; }
    public required string Ssid { get; init; }
    public string? Bssid { get; init; }
    public int SignalQualityPercent { get; init; }
    public string? Protocol { get; init; }
    public string? Security { get; init; }
    public int? Channel { get; init; }
    public string? FrequencyBand { get; init; }
    public double? RxRateMbps { get; init; }
    public double? TxRateMbps { get; init; }
}
