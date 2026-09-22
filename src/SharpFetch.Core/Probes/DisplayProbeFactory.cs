using SharpFetch.Platforms.Common;
using SharpFetch.Platforms.Linux;
using SharpFetch.Platforms.MacOS;
using SharpFetch.Platforms.Windows;

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
