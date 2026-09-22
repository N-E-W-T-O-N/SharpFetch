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
        engine.Register(new DisplayModule());
        engine.Register(new MemoryModule());
        engine.Register(new SwapModule());
        engine.Register(new DiskModule());
        engine.Register(new LocalIpModule());
        engine.Register(new WifiModule());
        engine.Register(new UptimeModule(probe));

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
            var orderList = enabledModules.ToList();
            query = query
                .Where(m => orderList.Contains(m.Metadata.Key, StringComparer.OrdinalIgnoreCase))
                .OrderBy(m => orderList.FindIndex(k => k.Equals(m.Metadata.Key, StringComparison.OrdinalIgnoreCase)));
        }
        else
        {
            query = query.OrderBy(m => m.Metadata.DefaultOrder);
        }

        if (disabledModules != null && disabledModules.Count > 0)
        {
            query = query.Where(m => !disabledModules.Contains(m.Metadata.Key, StringComparer.OrdinalIgnoreCase));
        }

        return query
            .SelectMany(m => m.Fetch())
            .ToList();
    }
}
