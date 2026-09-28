using SharpFetch.Core.Modules;
using SharpFetch.Core.Probes;
using SharpFetch.UI;

namespace SharpFetch.Cli;

internal class Program
{
    public static void Main(string[] args)
    {
        // 1. Parse CLI arguments with zero reflection and sub-microsecond latency
        CliOptions options = CliParser.Parse(args);

        // 2. Dispatch early-exit actions or execute fetch
        switch (options.Action)
        {
            case CliAction.ShowVersion:
                PrintVersion();
                return;

            case CliAction.ShowHelp:
                PrintHelp();
                return;

            case CliAction.ListModules:
                PrintModuleList();
                return;

            case CliAction.ShowError:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Error: {options.ErrorMessage}");
                Console.ResetColor();
                Environment.ExitCode = 1;
                return;

            case CliAction.RunFetch:
            default:
                RunFetch(options);
                break;
        }
    }

    private static void PrintVersion()
    {
        var version = typeof(Program).Assembly.GetName().Version;
        string versionStr = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
        Console.WriteLine($"SharpFetch v{versionStr}");
    }

    private static void RunFetch(CliOptions options)
    {
        // 1. Initialize OS probe and get baseline OS info
        var osProbe = OsProbeFactory.Create();
        var osInfo = osProbe.Detect();

        // 2. Initialize FetchEngine with default modules
        var engine = FetchEngine.CreateDefault(osProbe);

        // 3. Execute only filtered modules (avoids running unneeded probes)
        var results = engine.Execute(options.EnabledModules, options.DisabledModules);

        // 4. Render using Spectre.Console with CLI options
        var renderOptions = new RenderOptions(
            ShowLogo: !options.NoLogo,
            ShowColorPalette: options.ShowColorPalette,
            DisableColor: options.DisableColor,
            CustomLogo: options.CustomLogo,
            AccentColor: options.AccentColor,
            ShowDetails: options.ShowDetails
        );

        var descriptions = engine.Modules.ToDictionary(
            module => module.Metadata.Key,
            module => module.Metadata.Description,
            StringComparer.OrdinalIgnoreCase);
        ConsoleRenderer.Render(osInfo, results, renderOptions, descriptions);
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
        SharpFetch - High-performance cross-platform system information tool

        USAGE:
            sharpfetch [OPTIONS]

        OPTIONS:
            -h, --help               Show this help menu
            -v, --version            Display version information
            --list-modules           List all available telemetry modules
            -m, --modules <list>     Comma-separated list of modules to display (e.g. os,cpu,memory)
            --hide <list>            Comma-separated list of modules to hide
            -n, --no-logo            Hide OS ASCII logo
            --no-color               Disable all ANSI colors
            --no-palette             Hide ANSI color palette blocks
            --details                Show network addresses and adapter details, with module explanations
            -l, --logo <name>        Specify custom ASCII logo (e.g. arch, ubuntu, windows, macos, tux, debian)
            --color <name>           Override accent color
        """);
    }

    private static void PrintModuleList()
    {
        var modules = FetchEngine.CreateDefault().Modules
            .OrderBy(m => m.Metadata.DefaultOrder)
            .ToList();

        int keyWidth = modules.Max(m => m.Metadata.Key.Length);

        Console.WriteLine("Available Modules:");
        foreach (var module in modules)
        {
            Console.WriteLine($"  - {module.Metadata.Key.PadRight(keyWidth)} : {module.Metadata.Description}");
        }
    }
}
