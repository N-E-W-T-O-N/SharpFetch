using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms;

[SupportedOSPlatform("freebsd")]
public sealed partial class FreeBSDMemoryProbe : IMemoryProbe
{
    [LibraryImport("libc", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int sysctlbyname(
        string name,
        Span<byte> oldp,
        ref nuint oldlenp,
        IntPtr newp,
        nuint newlen);

    public MemoryInfo Detect()
    {
        ulong totalBytes = GetSysctlUInt64("hw.physmem");
        int pageSize = GetSysctlInt32("vm.stats.vm.v_page_size");
        if (pageSize <= 0) pageSize = 4096;

        int freePages = GetSysctlInt32("vm.stats.vm.v_free_count");
        ulong freeBytes = (ulong)freePages * (ulong)pageSize;
        ulong usedBytes = totalBytes > freeBytes ? totalBytes - freeBytes : 0;

        return new MemoryInfo
        {
            TotalBytes = totalBytes,
            UsedBytes = usedBytes
        };
    }

    private static ulong GetSysctlUInt64(string name)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        nuint len = (nuint)buffer.Length;
        return sysctlbyname(name, buffer, ref len, IntPtr.Zero, 0) == 0 && len >= sizeof(ulong)
            ? MemoryMarshal.Read<ulong>(buffer)
            : 0;
    }

    private static int GetSysctlInt32(string name)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        nuint len = (nuint)buffer.Length;
        return sysctlbyname(name, buffer, ref len, IntPtr.Zero, 0) == 0 && len >= sizeof(int)
            ? MemoryMarshal.Read<int>(buffer)
            : 0;
    }
}
