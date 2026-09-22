using SharpFetch.Core.Platforms;

namespace SharpFetch.Core.Probes;

public static class MemoryProbeFactory
{
    /// <summary>
    /// Returns a memory probe for the current platform, or null if none is
    /// implemented yet. macOS is intentionally unimplemented: an accurate
    /// "used" figure there needs Mach vm_statistics64 (wired/compressed/
    /// internal page accounting), which is materially more complex than the
    /// Windows/Linux paths and hasn't been verified against real hardware.
    /// </summary>
    public static IMemoryProbe? Create()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsMemoryProbe();
        }

        if (OperatingSystem.IsLinux())
        {
            return new LinuxMemoryProbe();
        }

        return null;
    }
}
