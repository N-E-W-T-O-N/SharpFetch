using SharpFetch.Core.Models;

namespace SharpFetch.UI;

public static class AsciiArt
{
    public static (string[] lines, string accentColor) GetLogo(OsFamily family, ReadOnlySpan<char> osName)
    {
        string name = osName.ToString().ToLowerInvariant();
        return family switch
        {
            OsFamily.Windows => GetWindowsLogo(),
            OsFamily.Linux => GetLinuxLogo(name),
            OsFamily.MacOS => GetMacLogo(),
            OsFamily.FreeBSD => GetLinuxLogo(name),
            OsFamily.Android => GetLinuxLogo(name),
            _ => GetGenericLogo()
        };
    }

    private static (string[] lines, string accentColor) GetWindowsLogo()
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

    private static (string[] lines, string accentColor) GetLinuxLogo(string nameLower)
    {
        

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

        if(nameLower.Contains("debian"))
        {
            string[] debianLines =
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
            return (debianLines, "red");
        }

        if(nameLower.Contains("fedora"))
        {
            string[] fedoraLines =
            [
               
            ];
            return (fedoraLines, "blue");
        }

        if(nameLower.Contains("gentoo"))
        {
            string[] gentooLines =
            [
                "[purple]      .'''.       [/]",
                "[purple]     :_\\/_:      [/]",
                "[purple] .''.: /\\ :.''.  [/]",
                "[purple]:_\\/_:'.::.' :  [/]",
                "[purple]: /\\ : :::::  :  [/]",
                "[purple] '..'   '::'   '  [/]"
            ];
            return (gentooLines, "purple");
        }

        if(nameLower.Contains("alpine"))
        {
            string[] alpineLines =
            [

            ];
            return (alpineLines, "green");
        }

        if (nameLower.Contains("android"))
        {
            // Ported from vendored neofetch's Android (bugdroid) ascii_data block.
            string[] androidLines =
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
            return (androidLines, "green");
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
