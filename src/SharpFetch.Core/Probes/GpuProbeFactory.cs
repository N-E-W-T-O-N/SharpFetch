using SharpFetch.Core.Platforms;

namespace SharpFetch.Core.Probes;

public static class GpuProbeFactory
{
    /// <summary>
    /// Returns a GPU probe for the current platform, or null if none is
    /// implemented yet. See CpuProbeFactory for why this doesn't fall back
    /// to a generic probe.
    /// </summary>
    public static IGpuProbe? Create()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsGpuProbe();
        }

        if (OperatingSystem.IsLinux())
        {
            return new LinuxGpuProbe();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacOsGpuProbe();
        }

        return null;
    }
}
