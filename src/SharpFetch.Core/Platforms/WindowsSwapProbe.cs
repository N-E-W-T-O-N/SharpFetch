using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms;

/// <summary>
/// Reads pagefile usage via NtQuerySystemInformation(SystemPageFileInformation).
/// There is no public Win32 equivalent that reports pure pagefile total/used
/// separate from physical RAM - GlobalMemoryStatusEx's TotalPageFile is a
/// commit-limit figure (RAM + pagefile combined), not the pagefile alone.
/// fastfetch uses the same NtQuerySystemInformation call for this reason.
///
/// SYSTEM_PAGEFILE_INFORMATION is undocumented by Microsoft but stable in
/// practice; the struct layout below (NextEntryOffset, TotalSize, TotalInUse,
/// PeakUsage, all uint32, in pages) matches public NT-internals references
/// and was empirically verified against this machine's real pagefile before
/// being wired in here.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed partial class WindowsSwapProbe : ISwapProbe
{
    private const int SystemPageFileInformation = 0x12;
    private const int StatusInfoLengthMismatch = unchecked((int)0xC0000004);

    [LibraryImport("ntdll.dll")]
    private static partial int NtQuerySystemInformation(
        int systemInformationClass,
        IntPtr systemInformation,
        uint systemInformationLength,
        out uint returnLength);

    public SwapInfo Detect()
    {
        uint pageSize = (uint)Environment.SystemPageSize;
        uint bufferSize = 4096;

        for (int attempt = 0; attempt < 5; attempt++)
        {
            IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
            try
            {
                int status = NtQuerySystemInformation(SystemPageFileInformation, buffer, bufferSize, out uint returnLength);

                if (status == StatusInfoLengthMismatch)
                {
                    bufferSize = Math.Max(returnLength, bufferSize * 2);
                    continue;
                }

                // Each entry needs at least 12 bytes (NextEntryOffset + TotalSize +
                // TotalInUse); a shorter success response would mean reading
                // uninitialized native memory below.
                if (status != 0 || returnLength < 12)
                {
                    return new SwapInfo { TotalBytes = 0, UsedBytes = 0 };
                }

                return SumEntries(buffer, returnLength, pageSize);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        return new SwapInfo { TotalBytes = 0, UsedBytes = 0 };
    }

    /// <summary>
    /// Walks the entry list NtQuerySystemInformation wrote into <paramref name="buffer"/>.
    /// Bounds every read against <paramref name="validLength"/> (the bytes the call
    /// actually reported writing) rather than trusting NextEntryOffset alone - a
    /// malformed or unexpectedly short response could otherwise walk past the end
    /// of the allocated native buffer into unmapped or adjacent memory.
    /// </summary>
    private static SwapInfo SumEntries(IntPtr buffer, uint validLength, uint pageSize)
    {
        ulong totalPages = 0;
        ulong usedPages = 0;

        int offset = 0;
        while (offset + 12 <= validLength)
        {
            uint nextEntryOffset = (uint)Marshal.ReadInt32(buffer, offset);
            totalPages += (uint)Marshal.ReadInt32(buffer, offset + 4);
            usedPages += (uint)Marshal.ReadInt32(buffer, offset + 8);

            if (nextEntryOffset == 0 || nextEntryOffset > int.MaxValue)
            {
                break;
            }

            offset += (int)nextEntryOffset;
        }

        return new SwapInfo
        {
            TotalBytes = totalPages * pageSize,
            UsedBytes = usedPages * pageSize
        };
    }
}
