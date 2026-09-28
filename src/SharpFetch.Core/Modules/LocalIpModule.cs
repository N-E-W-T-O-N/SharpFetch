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
        Description: "Shows active Ethernet, Wi-Fi, cellular, and dial-up connections; use --details for full adapter information.",
        DefaultOrder: 37,
        Icon: "󰩟"
    );

    public bool IsSupported => true;

    public IReadOnlyList<ModuleResult> Fetch()
    {
        var interfaces = _probe.DetectInterfaces()
            .Where(iface => iface.IsUp && iface.Type is NetworkType.Ethernet or NetworkType.Wifi or NetworkType.Cellular or NetworkType.Modem or NetworkType.Ppp)
            .ToList();
        if (interfaces.Count == 0)
            return Array.Empty<ModuleResult>();

        var results = new List<ModuleResult>();

        for (int i = 0; i < interfaces.Count; i++)
        {
            var iface = interfaces[i];
            var sb = new StringBuilder();

            sb.Append(iface.Ipv4Cidr ?? iface.Ipv4 ?? "IPv6 only (use --details)");






            string displayName = iface.IsVirtual
                ? "Network (vEthernet)"
                : iface.Type switch
                {
                    NetworkType.Ethernet => "Network (Ethernet)",
                    NetworkType.Wifi => "Network (Wi-Fi)",
                    NetworkType.Cellular => "Network (Phone: cellular)",
                    NetworkType.Modem or NetworkType.Ppp => "Network (Phone: dial-up)",
                    _ => "Network"
                };

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
