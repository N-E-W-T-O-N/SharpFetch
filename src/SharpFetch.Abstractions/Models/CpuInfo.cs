namespace SharpFetch.Core.Models;

public sealed record CpuInfo
{
    /// <summary>
    /// CPU brand string (e.g. "AMD Ryzen 7 7800X3D 8-Core Processor")
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// CPU vendor identifier (e.g. "GenuineIntel", "AuthenticAMD")
    /// </summary>
    public required string Vendor { get; init; }

    /// <summary>
    /// Number of physical cores, independent of simultaneous multithreading
    /// </summary>
    public required int PhysicalCores { get; init; }

    /// <summary>
    /// Number of logical processors (threads) the OS schedules onto
    /// </summary>
    public required int LogicalProcessors { get; init; }

    /// <summary>
    /// Reported base clock speed in MHz, or 0 if unavailable. This is a
    /// boot-time/static snapshot (Windows registry, /proc/cpuinfo) rather
    /// than a live reading.
    /// </summary>
    public int BaseClockMHz { get; init; }

    /// <summary>
    /// SMBIOS-reported max/turbo clock speed in MHz, or 0 if unavailable.
    /// Display code should prefer this over BaseClockMHz when present - it's
    /// what fastfetch and most fetch tools show as "the" CPU clock speed.
    /// </summary>
    public int MaxClockMHz { get; init; }
}
