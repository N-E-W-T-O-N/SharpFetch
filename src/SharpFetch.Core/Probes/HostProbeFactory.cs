using SharpFetch.Core.Platforms;
using SharpFetch.Core.Platforms.MacOS;

namespace SharpFetch.Core.Probes;

public static class HostProbeFactory
{
    public static IHostProbe? Create()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsHostProbe();
        }

        if (OperatingSystem.IsLinux())
        {
            return new LinuxHostProbe();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacOsHostProbe();
        }

        if (OperatingSystem.IsFreeBSD())
        {
            return new FreeBSDHostProbe();
        }

        return null;
    }
}
