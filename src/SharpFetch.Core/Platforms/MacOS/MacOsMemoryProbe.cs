using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms;

[SupportedOSPlatform("macos")]
public sealed partial class MacOsMemoryProbe : IMemoryProbe
{
    private const int HostVmInfo64 = 4;
    private const int HostVmInfo64Count = 38;

    [LibraryImport("libc", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int sysctlbyname(
        string name,
        Span<byte> oldp,
        ref nuint oldlenp,
        IntPtr newp,
        nuint newlen);

    [LibraryImport("libSystem.dylib")]
    private static partial nint mach_host_self();

    [LibraryImport("libSystem.dylib")]
    private static partial int host_statistics64(
        nint host,
        int flavor,
        Span<int> host_info_out,
        ref int host_info_outCnt);

    public MemoryInfo Detect()
    {
        ulong totalBytes = GetTotalBytes();
        ulong usedBytes = GetUsedBytes();

        if (usedBytes > totalBytes)
        {
            usedBytes = totalBytes;
        }

        return new MemoryInfo
        {
            TotalBytes = totalBytes,
            UsedBytes = usedBytes
        };
    }

    private static ulong GetTotalBytes()
    {
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        nuint len = (nuint)buffer.Length;
        return sysctlbyname("hw.memsize", buffer, ref len, IntPtr.Zero, 0) == 0 && len >= sizeof(ulong)
            ? MemoryMarshal.Read<ulong>(buffer)
            : 0;
    }

    private static ulong GetUsedBytes()
    {
        // 1. Get system page size (4096 on Intel, 16384 on Apple Silicon)
        ulong pageSize = 4096;
        Span<byte> pageBuf = stackalloc byte[sizeof(int)];
        nuint pageLen = (nuint)pageBuf.Length;
        if (sysctlbyname("hw.pagesize", pageBuf, ref pageLen, IntPtr.Zero, 0) == 0 && pageLen >= sizeof(int))
        {
            pageSize = (ulong)MemoryMarshal.Read<int>(pageBuf);
        }

        // 2. Query vm_statistics64
        nint host = mach_host_self();
        if (host == nint.Zero)
        {
            return 0;
        }

        // vm_statistics64 structure contains 38 32-bit words (or 64-bit page counts)
        Span<int> vmStat = stackalloc int[HostVmInfo64Count * 2];
        int count = HostVmInfo64Count;

        if (host_statistics64(host, HostVmInfo64, vmStat, ref count) == 0)
        {
            // Word offsets into vm_statistics64, verified against Apple's real
            // struct layout (apple-oss-distributions/xnu, osfmk/mach/vm_statistics.h):
            // free_count(0), active_count(1), inactive_count(2), wire_count(3) are
            // natural_t (1 word each); the run of uint64 fields between wire_count
            // and purgeable_count/speculative_count (zero_fill_count, reactivations,
            // pageins, pageouts, faults, cow_faults, lookups, hits, purges - 9
            // uint64 fields = 18 words) pushes compressor_page_count to word 32,
            // not 18 - word 18 is actually the low 32 bits of "hits" (a lifetime
            // VM-lookup counter, unrelated to memory usage).
            // active_count: natural_t at word offset 1
            // wire_count: natural_t at word offset 3
            // compressor_page_count: natural_t at word offset 32
            ulong activePages = (ulong)(uint)vmStat[1];
            ulong wiredPages = (ulong)(uint)vmStat[3];
            ulong compressedPages = (ulong)(uint)vmStat[32];

            return (activePages + wiredPages + compressedPages) * pageSize;
        }

        return 0;
    }
}
