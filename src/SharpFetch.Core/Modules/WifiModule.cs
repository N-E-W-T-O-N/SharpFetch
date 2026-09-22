using System.Text;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Modules;

public sealed class WifiModule : IFetchModule
{
    private readonly IWifiProbe _probe;

    public WifiModule(IWifiProbe? probe = null)
    {
        _probe = probe ?? WifiProbeFactory.Create();
    }

    public ModuleMetadata Metadata => new(
        Key: "Wifi",
        DisplayName: "Wi-Fi",
        Description: "Print connected Wi-Fi info (SSID, connection and security protocol)",
        DefaultOrder: 72,
        Icon: "󰤨"
    );

    public bool IsSupported => true;

    public IReadOnlyList<ModuleResult> Fetch()
    {
        var connections = _probe.DetectWifi();
        if (connections.Count == 0)
            return Array.Empty<ModuleResult>();

        var results = new List<ModuleResult>();

        for (int i = 0; i < connections.Count; i++)
        {
            var conn = connections[i];
            var sb = new StringBuilder();

            sb.Append(conn.Ssid);

            if (conn.SignalQualityPercent > 0)
            {
                sb.Append($" [{conn.SignalQualityPercent}%]");
            }

            if (!string.IsNullOrEmpty(conn.Protocol))
            {
                sb.Append($" - {conn.Protocol}");
            }

            if (!string.IsNullOrEmpty(conn.Security))
            {
                sb.Append($" - {conn.Security}");
            }

            if (!string.IsNullOrEmpty(conn.FrequencyBand))
            {
                sb.Append($" ({conn.FrequencyBand})");
            }
            else if (conn.Channel.HasValue)
            {
                sb.Append($" (Ch {conn.Channel.Value})");
            }

            if (conn.RxRateMbps.HasValue && conn.RxRateMbps.Value > 0)
            {
                sb.Append($" [{conn.RxRateMbps.Value:0} Mbps]");
            }

            string displayName = connections.Count == 1
                ? "Wi-Fi"
                : $"Wi-Fi ({conn.InterfaceName})";

            results.Add(new ModuleResult(
                Key: $"Wifi_{i}",
                DisplayName: displayName,
                FormattedValue: sb.ToString(),
                RawData: conn
            ));
        }

        return results;
    }
}
