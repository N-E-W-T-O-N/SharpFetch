namespace SharpFetch.Core.Modules;

public sealed record ModuleMetadata(
    string Key,
    string DisplayName,
    string Description,
    int DefaultOrder,
    string? Icon = null
);
