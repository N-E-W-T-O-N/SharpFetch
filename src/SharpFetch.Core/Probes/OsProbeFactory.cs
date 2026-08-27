using SharpFetch.Platforms.Common;
using SharpFetch.Platforms.Linux;
using SharpFetch.Platforms.MacOS;
using SharpFetch.Platforms.Windows;

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

        return new FallbackOsProbe();
    }
}
