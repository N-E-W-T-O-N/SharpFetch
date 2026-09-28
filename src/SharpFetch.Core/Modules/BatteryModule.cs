using System.Text;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Modules;

public sealed class BatteryModule : IFetchModule
{
    private readonly IBatteryProbe? _probe;

    public BatteryModule(IBatteryProbe? probe = null)
    {
        _probe = probe ?? BatteryProbeFactory.Create();
    }

    public ModuleMetadata Metadata => new(
        Key: "Battery",
        DisplayName: "Battery",
        Description: "Prints battery capacity, status, and charging telemetry",
        DefaultOrder: 11,
        Icon: ""
    );

    public bool IsSupported => _probe is not null;

    public IReadOnlyList<ModuleResult> Fetch()
    {
        if (_probe is null)
        {
            return [];
        }

        var batteries = _probe.DetectBatteries();
        if (batteries.Count == 0)
        {
            return [];
        }

        var results = new List<ModuleResult>(batteries.Count);

        for (int i = 0; i < batteries.Count; i++)
        {
            var battery = batteries[i];
            string displayName = batteries.Count > 1
                ? $"Battery ({battery.DeviceName})"
                : "Battery";

            var sb = new StringBuilder();
            sb.Append($"{battery.CapacityPercent:F0}%");

            if (battery.State != BatteryState.Unknown)
            {
                sb.Append(" [");
                if (battery.IsAcConnected && battery.State != BatteryState.Charging && battery.State != BatteryState.Full)
                {
                    sb.Append("AC Connected, ");
                }
                sb.Append(battery.State switch
                {
                    BatteryState.Charging => "Charging",
                    BatteryState.Discharging => "Discharging",
                    BatteryState.Full => "Full",
                    BatteryState.NotCharging => "Not charging",
                    _ => "Unknown"
                });
                sb.Append(']');
            }

            var extra = new List<string>();
            if (battery.PowerWatts is > 0)
            {
                extra.Add($"{battery.PowerWatts.Value:F1}W");
            }

            if (battery.TimeRemaining is { } tr && tr.TotalMinutes > 0 && tr.TotalHours < 100)
            {
                if (tr.Hours > 0)
                {
                    extra.Add($"{tr.Hours}h {tr.Minutes}m remaining");
                }
                else
                {
                    extra.Add($"{tr.Minutes}m remaining");
                }
            }

            if (extra.Count > 0)
            {
                sb.Append($" ({string.Join(", ", extra)})");
            }

            results.Add(new ModuleResult(
                Metadata.Key,
                displayName,
                sb.ToString(),
                battery
            ));
        }

        return results;
    }
}
