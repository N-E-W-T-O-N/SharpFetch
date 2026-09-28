using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Modules;

/// <summary>
/// The one module that genuinely reports multiple instances of the same
/// thing - one result per fixed disk volume, each with its own DisplayName
/// (e.g. "Disk (C:\)") rather than a single shared "Disk:" label.
/// </summary>
public sealed class DiskModule : IFetchModule
{
    private readonly IDiskProbe _probe;

    public DiskModule(IDiskProbe? probe = null)
    {
        _probe = probe ?? DiskProbeFactory.Create();
    }

    public ModuleMetadata Metadata => new(
        Key: "Disk",
        DisplayName: "Disk",
        Description: "Prints used/total space and filesystem for each fixed disk volume",
        DefaultOrder: 9,
        Icon: ""
    );

    public bool IsSupported => true;

    public IReadOnlyList<ModuleResult> Fetch()
    {
        IReadOnlyList<DiskVolumeInfo> volumes = _probe.Detect();

        var results = new List<ModuleResult>(volumes.Count);
        foreach (DiskVolumeInfo volume in volumes)
        {
            string key = $"{Metadata.Key}_{volume.MountPoint}";
            string displayName = $"{Metadata.DisplayName} ({volume.MountPoint})";

            string formatted = $"{ByteFormatter.Format(volume.UsedBytes)} / {ByteFormatter.Format(volume.TotalBytes)}" +
                $" ({ByteFormatter.PercentUsed(volume.UsedBytes, volume.TotalBytes)}%) - {volume.FileSystem}";

            results.Add(new ModuleResult(key, displayName, formatted, volume));
        }

        return results;
    }
}
