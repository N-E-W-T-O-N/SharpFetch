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
