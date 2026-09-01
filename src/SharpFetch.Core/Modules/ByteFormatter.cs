using System.Globalization;

namespace SharpFetch.Core.Modules;

/// <summary>
/// Formats a byte count with an independently-chosen binary-prefix unit -
/// used by Memory/Swap/Disk, which each display two byte counts ("used /
/// total") that can land in very different magnitudes (e.g. "60.32 MiB /
/// 24.00 GiB"), matching the convention most fetch tools use.
/// </summary>
internal static class ByteFormatter
{
    private const double KiB = 1024;
    private const double MiB = KiB * 1024;
    private const double GiB = MiB * 1024;
    private const double TiB = GiB * 1024;

    public static string Format(ulong bytes)
    {
        double value = bytes;
        return value switch
        {
            >= TiB => $"{(value / TiB).ToString("0.00", CultureInfo.InvariantCulture)} TiB",
            >= GiB => $"{(value / GiB).ToString("0.00", CultureInfo.InvariantCulture)} GiB",
            >= MiB => $"{(value / MiB).ToString("0.00", CultureInfo.InvariantCulture)} MiB",
            >= KiB => $"{(value / KiB).ToString("0.00", CultureInfo.InvariantCulture)} KiB",
            _ => $"{bytes} B"
        };
    }

    public static int PercentUsed(ulong used, ulong total) =>
        total == 0 ? 0 : (int)Math.Round(100.0 * used / total, MidpointRounding.AwayFromZero);
}
