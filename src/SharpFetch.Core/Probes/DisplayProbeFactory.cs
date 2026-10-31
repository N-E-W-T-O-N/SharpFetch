using SharpFetch.Core.Platforms.Common;
using SharpFetch.Core.Platforms.Linux;
using SharpFetch.Core.Platforms.MacOS;
using SharpFetch.Core.Platforms.Windows;

namespace SharpFetch.Core.Probes;

public static class DisplayProbeFactory
{
    public static IDisplayProbe Create()
    {
        if (OperatingSystem.IsWindows())
            return new WindowsDisplayProbe();

        if (OperatingSystem.IsLinux())
            return new LinuxDisplayProbe();

        if (OperatingSystem.IsMacOS())
            return new MacOsDisplayProbe();

        return new FallbackDisplayProbe();
    }
}
