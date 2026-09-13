using Spectre.Console;
using SharpFetch.Core.Models;
using SharpFetch.Core.Modules;

namespace SharpFetch.UI;

public static class ConsoleRenderer
{
    public static void Render(OsInfo os, IReadOnlyList<ModuleResult> results, RenderOptions? options = null)
    {
        if (options?.DisableColor == true)
        {
            AnsiConsole.Profile.Capabilities.ColorSystem = ColorSystem.NoColors;
        }

        var (logoLines, accentColor) = AsciiArt.GetLogo(os, options?.CustomLogo, options?.AccentColor);

        // Build right column lines
        var rightLines = new List<string>();

        // 1. Title module
        var titleResult = results.FirstOrDefault(r => r.Key == "Title");
        if (titleResult != null)
        {
            string rawTitle = titleResult.FormattedValue;
            int atIndex = rawTitle.IndexOf('@');
            if (atIndex > 0)
            {
                string user = rawTitle[..atIndex];
                string host = rawTitle[(atIndex + 1)..];
                rightLines.Add($"[bold {accentColor}]{user}[/][bold white]@[/][bold {accentColor}]{host}[/]");
            }
            else
            {
                rightLines.Add($"[bold {accentColor}]{rawTitle}[/]");
            }

            int titleLength = rawTitle.Length;
            rightLines.Add($"[grey]{new string('-', titleLength)}[/]");
        }

        // 2. Info rows (skipping Title module which is rendered as header)
        foreach (var item in results.Where(r => r.Key != "Title"))
        {
            rightLines.Add($"[bold {accentColor}]{item.DisplayName}:[/] {Markup.Escape(item.FormattedValue)}");
        }

        // 3. Color Palette Blocks
        if (options?.ShowColorPalette != false)
        {
            rightLines.Add("");
            rightLines.Add(
                "[black]███[/][red]███[/][green]███[/][yellow]███[/][blue]███[/][magenta]███[/][cyan]███[/][white]███[/]"
            );
            rightLines.Add(
                "[grey]███[/][red3]███[/][green3]███[/][yellow3]███[/][blue3]███[/][magenta3]███[/][cyan3]███[/][grey84]███[/]"
            );
        }

        string infoText = string.Join(Environment.NewLine, rightLines);

        var table = new Table
        {
            Border = TableBorder.None,
            ShowHeaders = false
        };

        if (options?.ShowLogo != false)
        {
            string logoText = string.Join(Environment.NewLine, logoLines);
            table.AddColumn(new TableColumn("Logo").PadRight(3));
            table.AddColumn(new TableColumn("Info"));
            table.AddRow(new Markup(logoText), new Markup(infoText));
        }
        else
        {
            table.AddColumn(new TableColumn("Info"));
            table.AddRow(new Markup(infoText));
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }
}
