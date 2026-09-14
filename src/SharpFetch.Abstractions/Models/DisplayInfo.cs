namespace SharpFetch.Core.Models;

public enum DisplayType
{
    Unknown,
    Builtin,
    External
}

public sealed record DisplayInfo
{
    public required string Name { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public double RefreshRate { get; init; }
    public int? DiagonalInches { get; init; }
    public DisplayType Type { get; init; }
    public bool IsPrimary { get; init; }
    public double ScaleFactor { get; init; } = 1.0;
    public string? HdrStatus { get; init; }
}
