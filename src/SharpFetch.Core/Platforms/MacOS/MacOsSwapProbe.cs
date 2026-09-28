using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms;

[SupportedOSPlatform("macos")]
public sealed partial class MacOsSwapProbe : ISwapProbe
{
    [LibraryImport("libc", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int sysctlbyname(
        string name,
        Span<byte> oldp,
        ref nuint oldlenp,
        IntPtr newp,
        nuint newlen);

    public SwapInfo Detect()
    {
        // struct xsw_usage:
        // uint64_t xsu_total (offset 0)
        // uint64_t xsu_avail (offset 8)
        // uint64_t xsu_used  (offset 16)
        // uint32_t xsu_pagesize (offset 24)
        // boolean_t xsu_encrypted (offset 28)
        Span<byte> buffer = stackalloc byte[32];
        nuint len = (nuint)buffer.Length;

        if (sysctlbyname("vm.swapusage", buffer, ref len, IntPtr.Zero, 0) == 0 && len >= 24)
        {
            ulong total = MemoryMarshal.Read<ulong>(buffer[..8]);
            ulong used = MemoryMarshal.Read<ulong>(buffer.Slice(16, 8));

            return new SwapInfo
            {
                TotalBytes = total,
                UsedBytes = used
            };
        }

        return new SwapInfo
        {
            TotalBytes = 0,
            UsedBytes = 0
        };
    }
}
