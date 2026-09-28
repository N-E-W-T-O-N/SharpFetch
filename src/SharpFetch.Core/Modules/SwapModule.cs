using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Modules;

public sealed class SwapModule : IFetchModule
{
    private readonly ISwapProbe? _probe;

    public SwapModule(ISwapProbe? probe = null)
    {
        _probe = probe ?? SwapProbeFactory.Create();
    }

    public ModuleMetadata Metadata => new(
        Key: "Swap",
        DisplayName: "Swap",
        Description: "Prints used/total swap (pagefile) space and usage percentage",
        DefaultOrder: 8,
        Icon: ""
    );

    public bool IsSupported => _probe is not null;

    public IReadOnlyList<ModuleResult> Fetch()
    {
        SwapInfo swap = _probe!.Detect();

        // No swap configured is common (many servers, some desktop setups) -
        // report nothing rather than a misleading "0 B / 0 B (0%)" line.
        if (swap.TotalBytes == 0)
        {
            return [];
        }

        string formatted = $"{ByteFormatter.Format(swap.UsedBytes)} / {ByteFormatter.Format(swap.TotalBytes)}" +
            $" ({ByteFormatter.PercentUsed(swap.UsedBytes, swap.TotalBytes)}%)";

        return [new ModuleResult(Metadata.Key, Metadata.DisplayName, formatted, swap)];
    }
}
