using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Modules;

public sealed class CpuModule : IFetchModule
{
    private readonly ICpuProbe? _probe;

    public CpuModule(ICpuProbe? probe = null)
    {
        _probe = probe ?? CpuProbeFactory.Create();
    }

    public ModuleMetadata Metadata => new(
        Key: "CPU",
        DisplayName: "CPU",
        Description: "Prints CPU model, physical/logical core counts, and clock speed",
        DefaultOrder: 4,
        Icon: ""
    );

    public bool IsSupported => _probe is not null;

    public IReadOnlyList<ModuleResult> Fetch()
    {
        CpuInfo cpu = _probe!.Detect();

        string formatted = $"{cpu.Model} ({cpu.PhysicalCores}C/{cpu.LogicalProcessors}T)";

        // Prefer the SMBIOS-reported max/turbo speed - it's what fastfetch and most
        // fetch tools show; the base clock is a fallback for when SMBIOS is unavailable.
        int freq = cpu.MaxClockMHz > 0 ? cpu.MaxClockMHz : cpu.BaseClockMHz;
        if (freq > 0)
        {
            formatted += $" @ {freq / 1000.0:0.00} GHz";
        }

        return [new ModuleResult(Metadata.Key, Metadata.DisplayName, formatted, cpu)];
    }
}
