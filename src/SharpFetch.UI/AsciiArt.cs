using SharpFetch.Core.Models;

namespace SharpFetch.UI;

public static class AsciiArt
{
    public static (string[] lines, string accentColor) GetLogo(OsInfo os, string? customLogo = null, string? customAccentColor = null)
    {
        return GetLogo(os.Family, os.Name, customLogo, customAccentColor);
    }

    public static (string[] lines, string accentColor) GetLogo(OsFamily family, ReadOnlySpan<char> osName, string? customLogo = null, string? customAccentColor = null)
    {
        var (lines, defaultAccent) = ResolveLogo(family, osName, customLogo);
        string finalAccent = !string.IsNullOrWhiteSpace(customAccentColor) ? customAccentColor : defaultAccent;
        return (lines, finalAccent);
    }

    private static (string[] lines, string accentColor) ResolveLogo(OsFamily family, ReadOnlySpan<char> osName, string? customLogo)
    {
        if (!string.IsNullOrWhiteSpace(customLogo))
        {
            return customLogo.Trim().ToLowerInvariant() switch
            {
                "windows" or "win" or "win11" or "win10" => GetWindowsLogo(),
                "arch" or "archlinux" => GetArchLogo(),
                "ubuntu" => GetUbuntuLogo(),
                "debian" => GetDebianLogo(),
                "gentoo" => GetGentooLogo(),
                "android" => GetAndroidLogo(),
                "linux" or "tux" => GetTuxLogo(),
                "macos" or "mac" or "apple" or "darwin" => GetMacLogo(),
                _ => GetGenericLogo()
            };
        }

        string name = osName.ToString().ToLowerInvariant();
        return family switch
        {
            OsFamily.Windows => GetWindowsLogo(),
            OsFamily.Linux => GetLinuxLogo(name),
            OsFamily.MacOS => GetMacLogo(),
            OsFamily.FreeBSD => GetLinuxLogo(name),
            OsFamily.Android => GetAndroidLogo(),
            _ => GetGenericLogo()
        };
    }

    private static (string[] lines, string accentColor) GetWindowsLogo()
    {
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

    private static (string[] lines, string accentColor) GetLinuxLogo(string nameLower)
    {
        if (nameLower.Contains("arch"))
        {
            return GetArchLogo();
        }

        if (nameLower.Contains("ubuntu"))
        {
            return GetUbuntuLogo();
        }

        if (nameLower.Contains("debian"))
        {
            return GetDebianLogo();
        }

        if (nameLower.Contains("gentoo"))
        {
            return GetGentooLogo();
        }

        if (nameLower.Contains("android"))
        {
            return GetAndroidLogo();
        }

        return GetTuxLogo();
    }

    private static (string[] lines, string accentColor) GetArchLogo()
    {
        string[] lines =
        [
            "[cyan]       /\\       [/]",
            "[cyan]      /  \\      [/]",
            "[cyan]     /\\   \\     [/]",
            "[cyan]    /      \\    [/]",
            "[cyan]   /   ,,   \\   [/]",
            "[cyan]  /   |  |  -\\  [/]",
            "[cyan] /_-''    ''-_\\ [/]"
        ];
        return (lines, "cyan1");
    }

    private static (string[] lines, string accentColor) GetUbuntuLogo()
    {
        string[] lines =
        [
            "[orange3]         _        [/]",
            "[orange3]     ---(_)       [/]",
            "[orange3] _/  ---  \\       [/]",
            "[orange3](_) |   |         [/]",
            "[orange3]  \\  --- _/       [/]",
            "[orange3]     ---(_)       [/]"
        ];
        return (lines, "orange3");
    }

    private static (string[] lines, string accentColor) GetDebianLogo()
    {
        string[] lines =
        [
            "[red]      _,met$$$$$gg.       [/]",
            "[red]   ,g$$$$$$$$$$$$$$$P.    [/]",
            "[red] ,g$$P\"\"       \"\"Y$$.\".   [/]",
            "[red],$$P'              `$$$.  [/]",
            "[red']$$P       ,ggs.     `$$b: [/]",
            "[red']d$$'     ,$P\"'   .    $$$ [/]",
            "[red']d$$'   ,$P'     ,    $$P [/]",
            "[red']d$$'  ,$P      ,     $$P [/]",
            "[red']d$$'  d$'     ,      $$P [/]",
            "[red']d$$'  $$.   -\"      $$P [/]",
            "[red']d$$'  `Y$b._       ,d$P' [/]",
            "[red']d$$'    `\"Y$$$$$$$$$P\"'  [/]"
        ];
        return (lines, "red");
    }

    private static (string[] lines, string accentColor) GetGentooLogo()
    {
        string[] lines =
        [
            "[purple]      .'''.       [/]",
            "[purple]     :_\\/_:      [/]",
            "[purple] .''.: /\\ :.''.  [/]",
            "[purple]:_\\/_:'.::.' :  [/]",
            "[purple]: /\\ : :::::  :  [/]",
            "[purple] '..'   '::'   '  [/]"
        ];
        return (lines, "purple");
    }

    private static (string[] lines, string accentColor) GetAndroidLogo()
    {
        string[] lines =
        [
            "[green]         -o          o-[/]",
            "[green]          +hydNNNNdyh+[/]",
            "[green]        +mMMMMMMMMMMMMm+[/]",
            "[green]      `dMM[/][white]m:[/][green]NMMMMMMN[/][white]:m[/][green]MMd`[/]",
            "[green]      hMMMMMMMMMMMMMMMMMMh[/]",
            "[green]  ..  yyyyyyyyyyyyyyyyyyyy  ..[/]",
            "[green].mMMm`MMMMMMMMMMMMMMMMMMMM`mMMm.[/]",
            "[green]:MMMM-MMMMMMMMMMMMMMMMMMMM-MMMM:[/]",
            "[green]:MMMM-MMMMMMMMMMMMMMMMMMMM-MMMM:[/]",
            "[green]:MMMM-MMMMMMMMMMMMMMMMMMMM-MMMM:[/]",
            "[green]:MMMM-MMMMMMMMMMMMMMMMMMMM-MMMM:[/]",
            "[green]-MMMM-MMMMMMMMMMMMMMMMMMMM-MMMM-[/]",
            "[green] +yy+ MMMMMMMMMMMMMMMMMMMM +yy+[/]",
            "[green]      mMMMMMMMMMMMMMMMMMMm[/]",
            "[green]      `/++MMMMh++hMMMM++/`[/]",
            "[green]          MMMMo  oMMMM[/]",
            "[green]          MMMMo  oMMMM[/]",
            "[green]          oNMm-  -mMNs[/]"
        ];
        return (lines, "green");
    }

    private static (string[] lines, string accentColor) GetTuxLogo()
    {
        string[] lines =
        [
            "[yellow]   .--.   [/]",
            "[yellow]  |o_o |  [/]",
            "[yellow]  |:_/ |  [/]",
            "[yellow] //   \\ \\ [/]",
            "[yellow](|     | )[/]",
            "[yellow]/'\\_   _/`\\[/]",
            "[yellow]\\___)=(___/[/]"
        ];
        return (lines, "yellow");
    }

    private static (string[] lines, string accentColor) GetMacLogo()
    {
        string[] lines =
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
        return (lines, "green1");
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
