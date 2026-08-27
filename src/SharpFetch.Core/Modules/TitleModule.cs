using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Modules;

public sealed class TitleModule : IFetchModule
{
    private readonly IOsProbe _probe;

    public TitleModule(IOsProbe? probe = null)
    {
        _probe = probe ?? OsProbeFactory.Create();
    }

    public ModuleMetadata Metadata => new(
        Key: "Title",
        DisplayName: "Title",
        Description: "Prints user@hostname title header",
        DefaultOrder: 1,
        Icon: null
    );

    public bool IsSupported => true;

    public ModuleResult Fetch()
    {
        OsInfo os = _probe.Detect();
        string title = $"{os.Username}@{os.Hostname}";
        return new ModuleResult(Metadata.Key, Metadata.DisplayName, title, os);
    }
}
