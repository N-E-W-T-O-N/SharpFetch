namespace SharpFetch.Core.Models;

/// <summary>
/// Aggregate total across every swap area (all pagefiles on Windows, all
/// swap partitions/files on Linux) - a system with several swap areas still
/// reports as one combined figure, matching a single "Swap:" display line.
/// </summary>
public sealed record SwapInfo
{
    public required ulong TotalBytes { get; init; }
    public required ulong UsedBytes { get; init; }
}
