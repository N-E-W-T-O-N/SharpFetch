using SharpFetch.Platforms.Common;
using SharpFetch.Platforms.Linux;
using SharpFetch.Platforms.MacOS;
using SharpFetch.Platforms.Windows;

namespace SharpFetch.Core.Probes;

public static class WifiProbeFactory
{
    public static IWifiProbe Create()
    {
        if (OperatingSystem.IsWindows())
            return new WindowsWifiProbe();

        if (OperatingSystem.IsLinux())
            return new LinuxWifiProbe();

        if (OperatingSystem.IsMacOS())
            return new MacOsWifiProbe();

        return new FallbackWifiProbe();
    }
}
