namespace SharpFetch.Core.Platforms;

/// <summary>
/// Parses a raw SMBIOS structure table (the DMTF-specified binary format) to
/// find the active CPU's Type 4 "Processor Information" MaxSpeed field - the
/// figure fastfetch and most other fetch tools display as CPU clock speed,
/// since it reflects the CPU's rated max/turbo speed rather than a live
/// reading or a stale boot-time snapshot (see WindowsCpuProbe/LinuxCpuProbe
/// for why those alternatives are misleading).
///
/// Field offsets below are the DMTF SMBIOS spec's stable, documented Type 4
/// layout, cross-checked against fastfetch's own FFSmbiosProcessorInfo
/// struct (which asserts its offsets at compile time) rather than derived
/// from memory alone.
/// </summary>
internal static class SmbiosTableParser
{
    private const byte TypeProcessorInfo = 4;
    private const byte TypeEndOfTable = 127;

    private const int OffsetProcessorType = 5;
    private const int OffsetMaxSpeed = 20; // uint16, MHz
    private const int OffsetStatus = 24;

    /// <summary>
    /// Returns the active CPU's SMBIOS-reported max speed in MHz, or 0 if it
    /// can't be found or looks implausible relative to <paramref name="baseSpeedMHz"/>.
    /// </summary>
    public static int FindProcessorMaxSpeedMHz(byte[] table, int baseSpeedMHz)
    {
        int speed = FindProcessorMaxSpeedRaw(table);

        // Some SMBIOS implementations report bogus values; fastfetch guards
        // against this the same way - assume the real max speed is between
        // the base speed and double it.
        if (speed <= 0 || (baseSpeedMHz > 0 && (speed < baseSpeedMHz || speed > baseSpeedMHz * 2)))
        {
            return 0;
        }

        return speed;
    }

    private static int FindProcessorMaxSpeedRaw(byte[] table)
    {
        int offset = 0;
        while (offset + 4 <= table.Length)
        {
            byte type = table[offset];
            byte length = table[offset + 1];
            if (length < 4 || type == TypeEndOfTable)
            {
                break;
            }

            // Both the structure's declared length AND its actual position in the
            // table matter: a truncated/corrupted table can declare a length that
            // extends past the bytes actually available.
            if (type == TypeProcessorInfo && length > OffsetStatus && offset + length <= table.Length)
            {
                byte processorType = table[offset + OffsetProcessorType];
                byte status = table[offset + OffsetStatus];
                // ProcessorType 0x03 = "Central Processor"; Status low 3 bits == 1 = "Enabled".
                if (processorType == 0x03 && (status & 0b0000_0111) == 1)
                {
                    return BitConverter.ToUInt16(table, offset + OffsetMaxSpeed);
                }
            }

            int next = SkipToNextStructure(table, offset, length);
            if (next < 0)
            {
                break;
            }

            offset = next;
        }

        return 0;
    }

    /// <summary>
    /// A structure's formatted section ("length" bytes) is followed by a
    /// sequence of NUL-terminated strings, itself terminated by an extra NUL
    /// (a double-NUL when there are no strings at all). This walks past both
    /// to find the next structure's header, mirroring fastfetch's
    /// ffSmbiosNextEntry with explicit bounds checks in place of trusting a
    /// NUL-terminated native buffer.
    /// </summary>
    private static int SkipToNextStructure(byte[] table, int structStart, byte formattedLength)
    {
        int p = structStart + formattedLength;
        if (p >= table.Length)
        {
            return -1;
        }

        if (table[p] != 0)
        {
            do
            {
                int strLen = 0;
                while (p + strLen < table.Length && table[p + strLen] != 0)
                {
                    strLen++;
                }

                p += strLen + 1;
                if (p >= table.Length)
                {
                    return -1;
                }
            } while (table[p] != 0);
        }
        else
        {
            p++;
        }

        return p + 1 <= table.Length ? p + 1 : -1;
    }
}
