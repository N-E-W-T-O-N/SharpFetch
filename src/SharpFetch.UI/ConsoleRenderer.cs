using Spectre.Console;
using System.Text.RegularExpressions;
using SharpFetch.Core.Models;
using SharpFetch.Core.Modules;

namespace SharpFetch.UI;

public static class ConsoleRenderer
{
    public static void Render(
        OsInfo os,
        IReadOnlyList<ModuleResult> results,
        RenderOptions? options = null,
        IReadOnlyDictionary<string, string>? moduleDescriptions = null)
    {
        var prevColorSystem = AnsiConsole.Profile.Capabilities.ColorSystem;
        try
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
                if (options?.ShowDetails != true && item.RawData is NetworkInterfaceInfo { IsVirtual: true })
                {
                    continue;
                }

                string value = FormatDetailedValue(item, options?.ShowDetails == true);
                rightLines.Add($"[bold {accentColor}]{Markup.Escape(item.DisplayName)}:[/] {EmphasizePercentages(value, accentColor)}");

                if (options?.ShowDetails == true && TryGetDescription(item.Key, moduleDescriptions, out string description))
                {
                    rightLines.Add($"[grey]{Markup.Escape(description)}[/]");
                }
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
        finally
        {
            if (options?.DisableColor == true)
            {
                AnsiConsole.Profile.Capabilities.ColorSystem = prevColorSystem;
            }
        }
    }

    private static string EmphasizePercentages(string value, string accentColor)
    {
        string escaped = Markup.Escape(value).ToString();
        return Regex.Replace(escaped, @"\d+(?:[.,]\d+)?%", match => $"[bold {accentColor}]{match.Value}[/]");
    }

    private static bool TryGetDescription(
        string resultKey,
        IReadOnlyDictionary<string, string>? descriptions,
        out string description)
    {
        description = string.Empty;
        if (descriptions is null)
            return false;

        if (descriptions.TryGetValue(resultKey, out string? exactDescription) && exactDescription is not null)
        {
            description = exactDescription;
            return true;
        }

        int suffixIndex = resultKey.IndexOf('_');
        if (suffixIndex > 0 &&
            descriptions.TryGetValue(resultKey[..suffixIndex], out string? moduleDescription) &&
            moduleDescription is not null)
        {
            description = moduleDescription;
            return true;
        }

        return false;
    }

    private static string FormatDetailedValue(ModuleResult item, bool showDetails)
    {
        if (!showDetails || item.RawData is not NetworkInterfaceInfo network)
            return item.FormattedValue;

        var details = new List<string>();
        if (!string.IsNullOrWhiteSpace(network.Ipv4)) details.Add($"IPv4: {network.Ipv4Cidr ?? network.Ipv4}");
        if (!string.IsNullOrWhiteSpace(network.Ipv6)) details.Add($"IPv6: {network.Ipv6}");
        if (!string.IsNullOrWhiteSpace(network.Name)) details.Add($"Interface: {network.Name}");
        if (!string.IsNullOrWhiteSpace(network.MacAddress)) details.Add($"MAC: {network.MacAddress}");
        if (network.SpeedBitsPerSecond > 0) details.Add($"Link: {network.FormattedSpeed}");
        if (network.IsDefaultGateway) details.Add("Default gateway");

        return details.Count == 0 ? item.FormattedValue : string.Join(" · ", details);
    }
}
