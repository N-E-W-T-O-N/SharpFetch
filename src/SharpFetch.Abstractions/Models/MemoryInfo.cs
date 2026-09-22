namespace SharpFetch.Core.Models;

public sealed record MemoryInfo
{
    public required ulong TotalBytes { get; init; }
    public required ulong UsedBytes { get; init; }
}
