using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Modules;

public sealed class GpuModule : IFetchModule
{
    private readonly IGpuProbe? _probe;

    public GpuModule(IGpuProbe? probe = null)
    {
        _probe = probe ?? GpuProbeFactory.Create();
    }

    public ModuleMetadata Metadata => new(
        Key: "GPU",
        DisplayName: "GPU",
        Description: "Prints detected graphics adapters and dedicated VRAM",
        DefaultOrder: 5,
        Icon: ""
    );

    public bool IsSupported => _probe is not null;

    public ModuleResult Fetch()
    {
        IReadOnlyList<GpuAdapterInfo> adapters = _probe!.Detect();

        string formatted = adapters.Count == 0
            ? "None detected"
            : string.Join(", ", adapters.Select(FormatAdapter));

        return new ModuleResult(Metadata.Key, Metadata.DisplayName, formatted, adapters);
    }

    private static string FormatAdapter(GpuAdapterInfo adapter)
    {
        var parts = new List<string>();

        if (adapter.CoreClockMHz is > 0)
        {
            parts.Add($"@ {adapter.CoreClockMHz.Value / 1000.0:0.00} GHz");
        }

        if (adapter.DedicatedVramBytes is > 0)
        {
            double mib = adapter.DedicatedVramBytes.Value / 1024.0 / 1024.0;
            parts.Add(mib >= 1024 ? $"({mib / 1024.0:0.00} GiB)" : $"({mib:0.00} MiB)");
        }

        if (adapter.IsIntegrated is bool integrated)
        {
            parts.Add(integrated ? "[Integrated]" : "[Discrete]");
        }

        return parts.Count == 0 ? adapter.Name : $"{adapter.Name} {string.Join(" ", parts)}";
    }
}
