using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Modules;

public sealed class HostModule : IFetchModule
{
    private readonly IHostProbe? _probe;

    public HostModule(IHostProbe? probe = null)
    {
        _probe = probe ?? HostProbeFactory.Create();
    }

    public ModuleMetadata Metadata => new(
        Key: "Host",
        DisplayName: "Host",
        Description: "Prints computer model, motherboard, or hardware platform",
        DefaultOrder: 2,
        Icon: "󰌢"
    );

    public bool IsSupported => _probe is not null;

    public IReadOnlyList<ModuleResult> Fetch()
    {
        if (_probe is null)
        {
            return [];
        }

        var info = _probe.Detect();
        if (info is null || string.IsNullOrWhiteSpace(info.Name))
        {
            return [];
        }

        string formatted = !string.IsNullOrWhiteSpace(info.Version)
            ? $"{info.Name} ({info.Version})"
            : info.Name;

        return [new ModuleResult(Metadata.Key, Metadata.DisplayName, formatted, info)];
    }
}
