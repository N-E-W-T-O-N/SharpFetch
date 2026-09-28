namespace SharpFetch.UI;

public sealed record RenderOptions(
    bool ShowLogo = true,
    bool ShowColorPalette = true,
    bool DisableColor = false,
    string? CustomLogo = null,
    string? AccentColor = null,
    bool ShowDetails = false
);
