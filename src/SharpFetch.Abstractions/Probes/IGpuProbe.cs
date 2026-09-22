using SharpFetch.Core.Models;

namespace SharpFetch.Core.Probes;

public interface IGpuProbe
{
    /// <summary>
    /// Detects all graphics adapters visible to the OS, in enumeration order.
    /// Returns an empty list if none could be detected - never throws for
    /// "no GPU found".
    /// </summary>
    IReadOnlyList<GpuAdapterInfo> Detect();
}
