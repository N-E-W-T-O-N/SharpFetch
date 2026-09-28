using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Modules;

public sealed class MemoryModule : IFetchModule
{
    private readonly IMemoryProbe? _probe;

    public MemoryModule(IMemoryProbe? probe = null)
    {
        _probe = probe ?? MemoryProbeFactory.Create();
    }

    public ModuleMetadata Metadata => new(
        Key: "Memory",
        DisplayName: "Memory",
        Description: "Prints used/total physical RAM and usage percentage",
        DefaultOrder: 7,
        Icon: ""
    );

    public bool IsSupported => _probe is not null;

    public IReadOnlyList<ModuleResult> Fetch()
    {
        MemoryInfo memory = _probe!.Detect();

        string formatted = $"{ByteFormatter.Format(memory.UsedBytes)} / {ByteFormatter.Format(memory.TotalBytes)}" +
            $" ({ByteFormatter.PercentUsed(memory.UsedBytes, memory.TotalBytes)}%)";

        return [new ModuleResult(Metadata.Key, Metadata.DisplayName, formatted, memory)];
    }
}
