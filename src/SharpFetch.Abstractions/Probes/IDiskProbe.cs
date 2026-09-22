using SharpFetch.Core.Models;

namespace SharpFetch.Core.Probes;

public interface IDiskProbe
{
    IReadOnlyList<DiskVolumeInfo> Detect();
}
