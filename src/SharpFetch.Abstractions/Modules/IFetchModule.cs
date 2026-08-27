namespace SharpFetch.Core.Modules;

public interface IFetchModule
{
    ModuleMetadata Metadata { get; }
    bool IsSupported { get; }
    ModuleResult Fetch();
}
