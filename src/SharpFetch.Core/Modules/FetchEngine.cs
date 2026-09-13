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
        engine.Register(new CpuModule());
        engine.Register(new GpuModule());
        engine.Register(new MemoryModule());
        engine.Register(new SwapModule());
        engine.Register(new DiskModule());

        return engine;
    }

    public void Register(IFetchModule module)
    {
        _modules.Add(module);
    }

    public IReadOnlyList<IFetchModule> Modules => _modules;

    public IReadOnlyList<ModuleResult> ExecuteAll() => Execute(null, null);

    public IReadOnlyList<ModuleResult> Execute(IReadOnlyCollection<string>? enabledModules, IReadOnlyCollection<string>? disabledModules)
    {
        var query = _modules.Where(m => m.IsSupported);

        if (enabledModules != null && enabledModules.Count > 0)
        {
            query = query.Where(m => enabledModules.Contains(m.Metadata.Key, StringComparer.OrdinalIgnoreCase));
        }

        if (disabledModules != null && disabledModules.Count > 0)
        {
            query = query.Where(m => !disabledModules.Contains(m.Metadata.Key, StringComparer.OrdinalIgnoreCase));
        }

        return query
            .OrderBy(m => m.Metadata.DefaultOrder)
            .SelectMany(m => m.Fetch())
            .ToList();
    }
}
