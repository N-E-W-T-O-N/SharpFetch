using SharpFetch.Core.Platforms.Android;
using SharpFetch.Core.Platforms.Common;
using SharpFetch.Core.Platforms.FreeBSD;
using SharpFetch.Core.Platforms.Linux;
using SharpFetch.Core.Platforms.MacOS;
using SharpFetch.Core.Platforms.Windows;

namespace SharpFetch.Core.Probes;

public static class OsProbeFactory
{
    public static IOsProbe Create()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsOsProbe();
        }

        // Checked before Linux: Android is Linux-based, so a Linux-first
        // order would shadow this branch entirely.
        if (OperatingSystem.IsAndroid())
        {
            return new AndroidProbe();
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
