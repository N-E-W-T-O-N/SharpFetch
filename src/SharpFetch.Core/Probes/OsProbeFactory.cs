using SharpFetch.Core.Platforms;

namespace SharpFetch.Core.Probes;

public static class OsProbeFactory
{
    public static IOsProbe Create()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsOsProbe();
        }

        if (OperatingSystem.IsLinux())
        {
            return new LinuxOsProbe();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacOsProbe();
        }

        if (OperatingSystem.IsFreeBSD())
        {
            return new FreeBSDProbe();
        }

        return new FallbackOsProbe();
    }
}
