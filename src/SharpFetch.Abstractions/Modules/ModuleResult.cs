namespace SharpFetch.Core.Modules;

public sealed record ModuleResult(
    string Key,
    string DisplayName,
    string FormattedValue,
    object? RawData = null
);
