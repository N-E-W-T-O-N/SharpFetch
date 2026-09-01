using System.Globalization;
using System.Runtime.Versioning;

namespace SharpFetch.Core.Platforms;

/// <summary>
/// Parses /proc/meminfo once per call into a name -> value(KiB) map. Despite
/// the "kB" suffix in the file, the kernel documents these as units of 1024
/// bytes (i.e. actually KiB) - a well-known quirk of this file's format.
/// </summary>
[SupportedOSPlatform("linux")]
internal static class ProcMemInfo
{
    public static Dictionary<string, ulong> Read()
    {
        var fields = new Dictionary<string, ulong>(StringComparer.Ordinal);

        try
        {
            foreach (string line in File.ReadLines("/proc/meminfo"))
            {
                int sep = line.IndexOf(':');
                if (sep <= 0)
                {
                    continue;
                }

                string key = line[..sep].Trim();
                string valuePart = line[(sep + 1)..].Trim();

                // Values are "<number> kB" (or occasionally bare for a few fields);
                // only the leading number is needed.
                int numEnd = 0;
                while (numEnd < valuePart.Length && char.IsAsciiDigit(valuePart[numEnd]))
                {
                    numEnd++;
                }

                if (numEnd > 0 && ulong.TryParse(valuePart[..numEnd], NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong value))
                {
                    fields[key] = value;
                }
            }
        }
        catch
        {
            // Return whatever was parsed so far (possibly empty); callers treat
            // missing keys as 0 via GetValueOrDefault.
        }

        return fields;
    }
}
