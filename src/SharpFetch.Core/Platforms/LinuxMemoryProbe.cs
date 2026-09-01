using System.Globalization;
using System.Runtime.Versioning;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms;

[SupportedOSPlatform("linux")]
public sealed class LinuxMemoryProbe : IMemoryProbe
{
    public MemoryInfo Detect()
    {
        var fields = ProcMemInfo.Read();

        ulong totalKb = fields.GetValueOrDefault("MemTotal");
        // MemAvailable (kernel 3.14+) already accounts for reclaimable cache/buffers;
        // older kernels need the classic Free+Buffers+Cached approximation.
        ulong availableKb = fields.TryGetValue("MemAvailable", out ulong avail)
            ? avail
            : fields.GetValueOrDefault("MemFree") + fields.GetValueOrDefault("Buffers") + fields.GetValueOrDefault("Cached");

        ulong usedKb = totalKb > availableKb ? totalKb - availableKb : 0;

        return new MemoryInfo
        {
            TotalBytes = totalKb * 1024,
            UsedBytes = usedKb * 1024
        };
    }
}
