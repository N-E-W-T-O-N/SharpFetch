using SharpFetch.Core.Modules;
using SharpFetch.Core.Probes;
using SharpFetch.UI;

namespace SharpFetch.Cli;

internal class Program
{
    public static void Main(string[] args)
    {
        // 1. Initialize OS probe and get baseline OS info
        var osProbe = OsProbeFactory.Create();
        var osInfo = osProbe.Detect();

        // 2. Initialize FetchEngine with default modules
        var engine = FetchEngine.CreateDefault(osProbe);

        // 3. Execute all modules
        var results = engine.ExecuteAll();

        // 4. Render using Spectre.Console
        ConsoleRenderer.Render(osInfo, results);
    }
}
