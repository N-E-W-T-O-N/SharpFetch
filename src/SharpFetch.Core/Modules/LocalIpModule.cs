using System.Text;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;
using SharpFetch.Platforms.Common;

namespace SharpFetch.Core.Modules;

public sealed class LocalIpModule : IFetchModule
{
    private readonly INetworkProbe _probe;

    public LocalIpModule(INetworkProbe? probe = null)
    {
        _probe = probe ?? new NetworkProbe();
    }

    public ModuleMetadata Metadata => new(
        Key: "LocalIp",
        DisplayName: "Local IP",
        Description: "List local IP addresses (v4 or v6), MAC addresses, link speeds, etc.",
        DefaultOrder: 37,
        Icon: "󰩟"
    );

    public bool IsSupported => true;

    public IReadOnlyList<ModuleResult> Fetch()
    {
        var interfaces = _probe.DetectInterfaces();
        if (interfaces.Count == 0)
            return Array.Empty<ModuleResult>();

        var results = new List<ModuleResult>();

        for (int i = 0; i < interfaces.Count; i++)
        {
            var iface = interfaces[i];
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(iface.Ipv4))
            {
                sb.Append(iface.Ipv4);
            }

            if (!string.IsNullOrEmpty(iface.Ipv6))
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(iface.Ipv6);
            }

            if (!string.IsNullOrEmpty(iface.MacAddress))
            {
                sb.Append($" ({iface.MacAddress})");
            }

            if (iface.SpeedBitsPerSecond > 0)
            {
                sb.Append($" [{iface.FormattedSpeed}]");
            }

            if (iface.IsDefaultGateway)
            {
                sb.Append(" *");
            }

            string displayName = interfaces.Count == 1
                ? "Local IP"
                : $"Local IP ({iface.Name})";

            results.Add(new ModuleResult(
                Key: $"LocalIp_{i}",
                DisplayName: displayName,
                FormattedValue: sb.ToString(),
                RawData: iface
            ));
        }

        return results;
    }
}
