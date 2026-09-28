using SharpFetch.Core.Platforms;
using SharpFetch.Core.Platforms.MacOS;

namespace SharpFetch.Core.Probes;

public static class BatteryProbeFactory
{
    public static IBatteryProbe? Create()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsBatteryProbe();
        }

        if (OperatingSystem.IsLinux())
        {
            return new LinuxBatteryProbe();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacOsBatteryProbe();
        }

        if (OperatingSystem.IsFreeBSD())
        {
            return new FreeBSDBatteryProbe();
        }

        return null;
    }
}
