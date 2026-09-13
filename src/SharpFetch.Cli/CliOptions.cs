namespace SharpFetch.Cli;

public sealed class CliOptions
{
    public CliAction Action { get; set; } = CliAction.RunFetch;

    // 1. Boolean Flags
    public bool NoLogo { get; set; }
    public bool ShowColorPalette { get; set; } = true;
    public bool DisableColor { get; set; }

    // 2. String Options
    public string? CustomLogo { get; set; }
    public string? AccentColor { get; set; }
    public string? ConfigPath { get; set; }

    // 3. List Options
    public List<string> EnabledModules { get; } = [];
    public List<string> DisabledModules { get; } = [];
}
