using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Modules;

public sealed class OsModule : IFetchModule
{
    private readonly IOsProbe _probe;

    public OsModule(IOsProbe? probe = null)
    {
        _probe = probe ?? OsProbeFactory.Create();
    }

    public ModuleMetadata Metadata => new(
        Key: "OS",
        DisplayName: "OS",
        Description: "Prints operating system name, version, and architecture",
        DefaultOrder: 3,
        Icon: ""
    );

    public bool IsSupported => true;

    public ModuleResult Fetch()
    {
        OsInfo os = _probe.Detect();
        string formatted = $"{os.PrettyName} [{os.Architecture}]";
        return new ModuleResult(Metadata.Key, Metadata.DisplayName, formatted, os);
    }
}
