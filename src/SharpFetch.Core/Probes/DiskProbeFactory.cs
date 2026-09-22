using SharpFetch.Core.Platforms;

namespace SharpFetch.Core.Probes;

public static class DiskProbeFactory
{
    // DriveInfo is cross-platform BCL, so unlike the other factories this
    // always has an implementation - no OS branching, never null.
    public static IDiskProbe Create() => new DriveInfoDiskProbe();
}
