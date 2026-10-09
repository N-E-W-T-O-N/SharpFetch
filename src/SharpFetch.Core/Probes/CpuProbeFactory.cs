using SharpFetch.Core.Platforms;

namespace SharpFetch.Core.Probes;

public static class CpuProbeFactory
{
    /// <summary>
    /// Returns a CPU probe for the current platform, or null if none is
    /// implemented yet - callers should treat null as "unsupported" rather
    /// than falling back to a generic probe, since there is no reliable
    /// generic way to read CPU identity/topology across platforms.
    /// </summary>
    public static ICpuProbe? Create()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsCpuProbe();
        }
        if (OperatingSystem.IsAndroid())
        {
            return new AndroidCpuProbe();
        })
        if (OperatingSystem.IsLinux())
        {
            return new LinuxCpuProbe();
        }
        if(OperatingSystem.IsIOS())
        {
            return new IosCpuProbe();
        }
        if(OperatingSystem.IsTvOS())
        {
            return new TvOsCpuProbe();
        })
        if (OperatingSystem.IsMacOS())
        {
            return new MacOsCpuProbe();
        }

        if (OperatingSystem.IsFreeBSD())
        {
            return new FreeBSDCpuProbe();
        }

        return null;
    }
}
