using System.Text;

namespace SharpFetch.Platforms.Common;

public static class EdidParser
{
    private static readonly byte[] EdidHeader = [0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00];

    public static bool TryParse(ReadOnlySpan<byte> edid, out string? monitorName, out int? diagonalInches)
    {
        monitorName = null;
        diagonalInches = null;

        if (edid.Length < 128)
            return false;

        // Verify EDID magic header
        if (!edid[..8].SequenceEqual(EdidHeader))
            return false;

        // Physical dimensions in cm (offsets 21 and 22)
        int widthCm = edid[21];
        int heightCm = edid[22];
        if (widthCm > 0 && heightCm > 0)
        {
            double diagonal = Math.Sqrt(widthCm * widthCm + heightCm * heightCm) / 2.54;
            if (diagonal > 2.0)
            {
                diagonalInches = (int)Math.Round(diagonal);
            }
        }

        // Search 4 descriptor blocks (18 bytes each at offsets 54, 72, 90, 108)
        for (int offset = 54; offset <= 108; offset += 18)
        {
            // Monitor Name Descriptor type is 0xFC (header: 00 00 00 FC 00)
            if (edid[offset] == 0x00 && edid[offset + 1] == 0x00 &&
                edid[offset + 2] == 0x00 && edid[offset + 3] == 0xFC)
            {
                var nameSpan = edid.Slice(offset + 5, 13);
                string raw = Encoding.ASCII.GetString(nameSpan);
                string clean = raw.Trim('\0', '\r', '\n', ' ');
                if (!string.IsNullOrEmpty(clean))
                {
                    monitorName = clean;
                    break;
                }
            }
        }

        return monitorName != null || diagonalInches != null;
    }
}
