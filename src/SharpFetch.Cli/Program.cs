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
            AccentColor: options.AccentColor
        );

        ConsoleRenderer.Render(osInfo, results, renderOptions);
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
            -l, --logo <name>        Specify custom ASCII logo (e.g. arch, ubuntu, windows, macos, tux, debian)
            --color <name>           Override accent color
        """);
    }

    private static void PrintModuleList()
    {
        Console.WriteLine("""
        Available Modules:
          - Title    : user@hostname title header
          - OS       : Operating system name, version, and architecture
          - Kernel   : OS kernel version
          - Uptime   : System uptime
          - CPU      : CPU model and specifications
          - GPU      : GPU graphics adapters
          - Memory   : Physical memory (RAM) usage
          - Swap     : Swap space usage
          - Disk     : Disk volume capacity and usage
        """);
    }
}
