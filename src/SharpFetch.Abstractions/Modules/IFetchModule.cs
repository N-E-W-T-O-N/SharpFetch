namespace SharpFetch.Core.Modules;

public interface IFetchModule
{
    ModuleMetadata Metadata { get; }
    bool IsSupported { get; }

    /// <summary>
    /// Returns the rendered line(s) for this module. Most modules return
    /// exactly one result; a module reporting several instances of the same
    /// kind of thing (e.g. one line per disk volume) returns one result per
    /// instance, each with its own Key/DisplayName.
    /// </summary>
    IReadOnlyList<ModuleResult> Fetch();
}
