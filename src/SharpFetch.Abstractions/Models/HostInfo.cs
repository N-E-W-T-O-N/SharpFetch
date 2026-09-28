namespace SharpFetch.Core.Models;

public sealed record HostInfo
{
    public required string Name { get; init; }
    public string? Version { get; init; }
    public string? Vendor { get; init; }
    public string? Family { get; init; }
}
