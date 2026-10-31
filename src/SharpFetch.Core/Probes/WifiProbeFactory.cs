using SharpFetch.Core.Platforms.Common;
using SharpFetch.Core.Platforms.Linux;
using SharpFetch.Core.Platforms.MacOS;
using SharpFetch.Core.Platforms.Windows;

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
