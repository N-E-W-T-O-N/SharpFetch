using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Modules;

public sealed class UptimeModule : IFetchModule
{
    private readonly IOsProbe _probe;

    public UptimeModule(IOsProbe? probe = null)
    {
        _probe = probe ?? OsProbeFactory.Create();
    }

    public ModuleMetadata Metadata => new(
        Key: "Uptime",
        DisplayName: "Uptime",
        Description: "Prints system uptime",
        DefaultOrder: 10,
        Icon: ""
    );

    public bool IsSupported => true;

    public IReadOnlyList<ModuleResult> Fetch()
    {
        OsInfo os = _probe.Detect();
        string formatted = FormatUptime(os.Uptime);
        return [new ModuleResult(Metadata.Key, Metadata.DisplayName, formatted, os)];
    }

    private static string FormatUptime(TimeSpan uptime)
    {
        var parts = new List<string>();

        if (uptime.Days > 0)
        {
            parts.Add($"{uptime.Days} day{(uptime.Days != 1 ? "s" : "")}");
        }

        if (uptime.Hours > 0 || uptime.Days > 0)
        {
            parts.Add($"{uptime.Hours} hour{(uptime.Hours != 1 ? "s" : "")}");
        }

        parts.Add($"{uptime.Minutes} min{(uptime.Minutes != 1 ? "s" : "")}");

        return string.Join(", ", parts);
    }
}
