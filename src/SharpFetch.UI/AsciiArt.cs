using SharpFetch.Core.Models;

namespace SharpFetch.UI;

public static class AsciiArt
{
    public static (string[] lines, string accentColor) GetLogo(OsInfo os)
    {
        return os.Family switch
        {
            OsFamily.Windows => GetWindowsLogo(os),
            OsFamily.Linux => GetLinuxLogo(os),
            OsFamily.MacOS => GetMacLogo(),
            _ => GetGenericLogo()
        };
    }

    private static (string[] lines, string accentColor) GetWindowsLogo(OsInfo os)
    {
        // Modern Windows 11/10 4-quadrant style
        string[] lines =
        [
            "[blue]################  ################[/]",
            "[blue]################  ################[/]",
            "[blue]################  ################[/]",
            "[blue]################  ################[/]",
            "[blue]################  ################[/]",
            "[blue]################  ################[/]",
            "",
            "[blue]################  ################[/]",
            "[blue]################  ################[/]",
            "[blue]################  ################[/]",
            "[blue]################  ################[/]",
            "[blue]################  ################[/]",
            "[blue]################  ################[/]"
        ];

        return (lines, "cyan1");
    }

    private static (string[] lines, string accentColor) GetLinuxLogo(OsInfo os)
    {
        string nameLower = os.Name.ToLowerInvariant();

        if (nameLower.Contains("arch"))
        {
            string[] archLines =
            [
                "[cyan]       /\\       [/]",
                "[cyan]      /  \\      [/]",
                "[cyan]     /\\   \\     [/]",
                "[cyan]    /      \\    [/]",
                "[cyan]   /   ,,   \\   [/]",
                "[cyan]  /   |  |  -\\  [/]",
                "[cyan] /_-''    ''-_\\ [/]"
            ];
            return (archLines, "cyan1");
        }

        if (nameLower.Contains("ubuntu"))
        {
            string[] ubuntuLines =
            [
                "[orange3]         _        [/]",
                "[orange3]     ---(_)       [/]",
                "[orange3] _/  ---  \\       [/]",
                "[orange3](_) |   |         [/]",
                "[orange3]  \\  --- _/       [/]",
                "[orange3]     ---(_)       [/]"
            ];
            return (ubuntuLines, "orange3");
        }

        // Generic Linux Tux
        string[] tuxLines =
        [
            "[yellow]   .--.   [/]",
            "[yellow]  |o_o |  [/]",
            "[yellow]  |:_/ |  [/]",
            "[yellow] //   \\ \\ [/]",
            "[yellow](|     | )[/]",
            "[yellow]/'\\_   _/`\\[/]",
            "[yellow]\\___)=(___/[/]"
        ];
        return (tuxLines, "yellow");
    }

    private static (string[] lines, string accentColor) GetMacLogo()
    {
        string[] macLines =
        [
            "[green]                    'c.          [/]",
            "[green]                 ,xNMM.          [/]",
            "[green]               .OMMMMo           [/]",
            "[yellow]               OMMM0,            [/]",
            "[yellow]     .;loddo:' loolloddol;.      [/]",
            "[yellow]   cKMMMMMMMMMMNWMMMMMMMMMM0:    [/]",
            "[red] .KMMMMMMMMMMMMMMMMMMMMMMMWd.   [/]",
            "[red] XMMMMMMMMMMMMMMMMMMMMMMMX.     [/]",
            "[magenta];MMMMMMMMMMMMMMMMMMMMMMMM:      [/]",
            "[magenta]:MMMMMMMMMMMMMMMMMMMMMMMM:      [/]",
            "[blue].MMMMMMMMMMMMMMMMMMMMMMMMX.     [/]",
            "[blue] kMMMMMMMMMMMMMMMMMMMMMMMMWd.   [/]",
            "[cyan]  'XMMMMMMMMMMMMMMMMMMMMMMMMMMk [/]",
            "[cyan]    'okKNX0KWMMMMMNKXNWNXOd;.   [/]"
        ];
        return (macLines, "green1");
    }

    private static (string[] lines, string accentColor) GetGenericLogo()
    {
        string[] lines =
        [
            "[grey]  .---.  [/]",
            "[grey] /     \\ [/]",
            "[grey]| () () |[/]",
            "[grey] \\  _  / [/]",
            "[grey]  `---`  [/]"
        ];
        return (lines, "white");
    }
}
