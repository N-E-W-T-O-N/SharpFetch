using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms;

[SupportedOSPlatform("windows")]
public sealed partial class WindowsCpuProbe : ICpuProbe
{
    private const int RelationProcessorCore = 0;

    // Multi-char firmware table provider signature 'RSMB', per
    // GetSystemFirmwareTable's documented convention (same encoding as 'ACPI'/'FIRM').
    private const uint FirmwareTableProviderRsmb = 0x52534D42;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetLogicalProcessorInformationEx(
        int relationshipType,
        IntPtr buffer,
        ref uint returnedLength);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial uint GetSystemFirmwareTable(
        uint firmwareTableProviderSignature,
        uint firmwareTableId,
        IntPtr buffer,
        uint bufferSize);

    public CpuInfo Detect()
    {
        string model = "Unknown CPU";
        string vendor = string.Empty;
        int baseClockMHz = 0;

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            if (key != null)
            {
                model = (key.GetValue("ProcessorNameString") as string)?.Trim() ?? model;
                vendor = key.GetValue("VendorIdentifier") as string ?? vendor;

                if (key.GetValue("~MHz") is int mhz)
                {
                    baseClockMHz = mhz;
                }
            }
        }
        catch
        {
            // Ignore registry read errors and fall back to the defaults above.
        }

        return new CpuInfo
        {
            Model = model,
            Vendor = vendor,
            PhysicalCores = GetPhysicalCoreCount(),
            LogicalProcessors = Environment.ProcessorCount,
            BaseClockMHz = baseClockMHz,
            MaxClockMHz = GetMaxClockMHz(baseClockMHz)
        };
    }

    /// <summary>
    /// Counts physical cores via GetLogicalProcessorInformationEx(RelationProcessorCore).
    /// Each returned SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX record represents one physical
    /// core regardless of how many logical processors (hyperthreads) it exposes - there is
    /// no registry value for this, so the buffer is walked manually using each record's
    /// leading (Relationship: int32, Size: int32) header to find the next record.
    /// </summary>
    private static int GetPhysicalCoreCount()
    {
        uint returnedLength = 0;
        GetLogicalProcessorInformationEx(RelationProcessorCore, IntPtr.Zero, ref returnedLength);
        if (returnedLength == 0)
        {
            return Environment.ProcessorCount;
        }

        IntPtr buffer = Marshal.AllocHGlobal((int)returnedLength);
        try
        {
            if (!GetLogicalProcessorInformationEx(RelationProcessorCore, buffer, ref returnedLength))
            {
                return Environment.ProcessorCount;
            }

            int count = 0;
            long offset = 0;
            while (offset < returnedLength)
            {
                int size = Marshal.ReadInt32(buffer, (int)offset + 4);
                if (size <= 0)
                {
                    break;
                }

                count++;
                offset += size;
            }

            return count > 0 ? count : Environment.ProcessorCount;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <summary>
    /// Reads SMBIOS Type 4 MaxSpeed via GetSystemFirmwareTable - the registry's ~MHz
    /// value (used for BaseClockMHz above) is a stale boot-time snapshot that can read
    /// far below the CPU's actual rated/turbo speed. Empirically verified on hardware:
    /// this returns 4900 (4.90 GHz) on a machine where the registry ~MHz reads ~3264.
    /// </summary>
    private static int GetMaxClockMHz(int baseClockMHz)
    {
        uint size = GetSystemFirmwareTable(FirmwareTableProviderRsmb, 0, IntPtr.Zero, 0);
        if (size == 0)
        {
            return 0;
        }

        IntPtr buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            uint written = GetSystemFirmwareTable(FirmwareTableProviderRsmb, 0, buffer, size);
            if (written == 0)
            {
                return 0;
            }

            // RawSMBIOSData header: Used20CallingMethod(1) + SMBIOSMajorVersion(1) +
            // SMBIOSMinorVersion(1) + DmiRevision(1) + Length(4), then raw table bytes.
            const int headerSize = 8;
            if (written <= headerSize)
            {
                return 0;
            }

            byte[] raw = new byte[written];
            Marshal.Copy(buffer, raw, 0, (int)written);

            uint tableLength = BitConverter.ToUInt32(raw, 4);
            int available = raw.Length - headerSize;
            int length = (int)Math.Min(tableLength, (uint)available);
            if (length <= 0)
            {
                return 0;
            }

            byte[] table = new byte[length];
            Array.Copy(raw, headerSize, table, 0, length);

            return SmbiosTableParser.FindProcessorMaxSpeedMHz(table, baseClockMHz);
        }
        catch
        {
            return 0;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}
