namespace SharpFetch.Core.Platforms.Linux;

/// <summary>
/// Resolves PCI vendor/device IDs against the system pci.ids database (the one
/// lspci uses, shipped by hwdata/pciutils) when it is installed. The file is
/// ~1.4 MB, so every ID is resolved in a single pass that stops as soon as all
/// requested vendor blocks have been read.
/// </summary>
internal static class PciIds
{
    private static readonly string[] DatabasePaths =
    [
        "/usr/share/hwdata/pci.ids",
        "/usr/share/misc/pci.ids",
        "/usr/share/pci.ids",
        "/var/lib/pciutils/pci.ids",
    ];

    /// <summary>
    /// Normalizes a sysfs ID ("0x10de") to pci.ids form ("10de"), or returns
    /// null if it is not a 4-digit hex ID (e.g. an unreadable sysfs file).
    /// </summary>
    public static string? Normalize(string id)
    {
        string trimmed = id.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? id[2..] : id;
        return trimmed.Length == 4 && trimmed.All(char.IsAsciiHexDigit) ? trimmed.ToLowerInvariant() : null;
    }

    /// <summary>
    /// Looks up normalized (vendor, device) pairs. Pairs whose vendor is unknown
    /// are absent from the result; DeviceName is null when only the vendor is known.
    /// </summary>
    public static Dictionary<(string Vendor, string Device), (string VendorName, string? DeviceName)> Lookup(
        IReadOnlyCollection<(string Vendor, string Device)> ids)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        foreach (string path in DatabasePaths)
        {
            if (!File.Exists(path)) continue;

            try
            {
                return Parse(File.ReadLines(path), ids);
            }
            catch
            {
                // Unreadable database; try the next location.
            }
        }

        return [];
    }

    internal static Dictionary<(string Vendor, string Device), (string VendorName, string? DeviceName)> Parse(
        IEnumerable<string> lines, IReadOnlyCollection<(string Vendor, string Device)> ids)
    {
        var wantedVendors = ids.Select(id => id.Vendor).ToHashSet(StringComparer.Ordinal);
        var wantedPairs = ids.ToHashSet();
        var vendorNames = new Dictionary<string, string>(StringComparer.Ordinal);
        var deviceNames = new Dictionary<(string, string), string>();

        string? currentVendor = null;
        foreach (string line in lines)
        {
            if (line.Length == 0 || line[0] == '#') continue;

            if (line[0] != '\t')
            {
                if (currentVendor is not null && vendorNames.Count == wantedVendors.Count) break;
                currentVendor = null;

                // The device-class section ("C 03  Display controller") follows
                // every vendor block, so nothing past it is a vendor.
                if (line.StartsWith("C ", StringComparison.Ordinal)) break;

                if (line.Length > 6 && line[4] == ' ' && wantedVendors.Contains(line[..4]))
                {
                    currentVendor = line[..4];
                    vendorNames[currentVendor] = line[4..].Trim();
                }
            }
            else if (currentVendor is not null && line.Length > 6 && line[1] != '\t' && line[5] == ' ')
            {
                // "\tdddd  Device name"; "\t\t" lines are subsystems and skipped.
                var pair = (currentVendor, line.Substring(1, 4));
                if (wantedPairs.Contains(pair))
                {
                    deviceNames[pair] = line[5..].Trim();
                }
            }
        }

        var results = new Dictionary<(string Vendor, string Device), (string VendorName, string? DeviceName)>();
        foreach (var id in wantedPairs)
        {
            if (vendorNames.TryGetValue(id.Vendor, out string? vendorName))
            {
                results[id] = (vendorName, deviceNames.GetValueOrDefault(id));
            }
        }

        return results;
    }
}
