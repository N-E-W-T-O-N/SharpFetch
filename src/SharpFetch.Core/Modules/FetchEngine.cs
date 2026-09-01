using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Modules;

public sealed class FetchEngine
{
    private readonly List<IFetchModule> _modules = [];

    public static FetchEngine CreateDefault(IOsProbe? osProbe = null)
    {
        var probe = osProbe ?? OsProbeFactory.Create();
        var engine = new FetchEngine();

        // Register core baseline modules
        engine.Register(new TitleModule(probe));
        engine.Register(new OsModule(probe));
        engine.Register(new KernelModule(probe));
        engine.Register(new CpuModule());
        engine.Register(new GpuModule());
        engine.Register(new MemoryModule());
        engine.Register(new SwapModule());
        engine.Register(new DiskModule());
        engine.Register(new UptimeModule(probe));

        return engine;
    }

    public void Register(IFetchModule module)
    {
        _modules.Add(module);
    }

    public IReadOnlyList<ModuleResult> ExecuteAll()
    {
        return _modules
            .Where(m => m.IsSupported)
            .OrderBy(m => m.Metadata.DefaultOrder)
            .SelectMany(m => m.Fetch())
            .ToList();
    }
}
