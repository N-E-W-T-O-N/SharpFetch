using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Modules;

public sealed class KernelModule : IFetchModule
{
    private readonly IOsProbe _probe;

    public KernelModule(IOsProbe? probe = null)
    {
        _probe = probe ?? OsProbeFactory.Create();
    }

    public ModuleMetadata Metadata => new(
        Key: "Kernel",
        DisplayName: "Kernel",
        Description: "Prints OS kernel version",
        DefaultOrder: 3,
        Icon: ""
    );

    public bool IsSupported => true;

    public ModuleResult Fetch()
    {
        OsInfo os = _probe.Detect();
        return new ModuleResult(Metadata.Key, Metadata.DisplayName, os.Kernel, os);
    }
}
