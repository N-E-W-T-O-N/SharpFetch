using System.Runtime.Versioning;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms;

[SupportedOSPlatform("linux")]
public sealed class LinuxSwapProbe : ISwapProbe
{
    public SwapInfo Detect()
    {
        var fields = ProcMemInfo.Read();

        ulong totalKb = fields.GetValueOrDefault("SwapTotal");
        ulong freeKb = fields.GetValueOrDefault("SwapFree");
        ulong usedKb = totalKb > freeKb ? totalKb - freeKb : 0;

        return new SwapInfo
        {
            TotalBytes = totalKb * 1024,
            UsedBytes = usedKb * 1024
        };
    }
}
