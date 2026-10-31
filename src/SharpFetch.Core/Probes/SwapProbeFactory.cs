using SharpFetch.Core.Platforms.Linux;
using SharpFetch.Core.Platforms.MacOS;
using SharpFetch.Core.Platforms.Windows;

namespace SharpFetch.Core.Probes;

public static class SwapProbeFactory
{
    /// <summary>
    /// Returns a swap probe for the current platform, or null if none is
    /// implemented yet. See MemoryProbeFactory for why macOS is excluded.
    /// </summary>
    public static ISwapProbe? Create()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsSwapProbe();
        }

        if (OperatingSystem.IsLinux())
        {
            return new LinuxSwapProbe();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacOsSwapProbe();
        }

        return null;
    }
}
