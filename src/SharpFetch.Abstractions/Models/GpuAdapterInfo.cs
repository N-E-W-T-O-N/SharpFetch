namespace SharpFetch.Core.Models;

public sealed record GpuAdapterInfo
{
    /// <summary>
    /// Enumeration order among detected adapters, starting at 0
    /// </summary>
    public required int Index { get; init; }

    /// <summary>
    /// Adapter model name (e.g. "NVIDIA GeForce RTX 4080")
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Adapter vendor/provider (e.g. "NVIDIA", "Intel Corporation")
    /// </summary>
    public required string Vendor { get; init; }

    /// <summary>
    /// Dedicated VRAM in bytes, or null if the platform doesn't report it.
    /// This is the adapter's own memory pool, distinct from any additional
    /// system RAM a driver can allocate as shared/non-local memory - some
    /// probing methods conflate the two (see WindowsGpuProbe's history) and
    /// report a much larger, misleading figure.
    /// </summary>
    public long? DedicatedVramBytes { get; init; }

    /// <summary>
    /// True if this is an integrated GPU sharing the CPU package, false if
    /// discrete, null if the platform can't determine which.
    /// </summary>
    public bool? IsIntegrated { get; init; }

    /// <summary>
    /// Core clock speed in MHz, or null if unavailable. Not currently
    /// populated on any platform: the generic OS-level query mechanisms
    /// (D3DKMT on Windows, PCI sysfs on Linux) don't expose it - fastfetch
    /// itself only gets this via vendor-specific driver libraries (Intel
    /// IGCL, NVIDIA NVAPI, AMD ADL), which is out of scope here. The field
    /// exists so a future vendor-specific probe has somewhere to put it.
    /// </summary>
    public int? CoreClockMHz { get; init; }
}
