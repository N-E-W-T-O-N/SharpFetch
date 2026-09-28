using System.Runtime.Versioning;
using Microsoft.Win32;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms;

[SupportedOSPlatform("windows")]
public sealed class WindowsHostProbe : IHostProbe
{
    private static readonly string[] GenericNames =
    [
        "system product name",
        "default string",
        "to be filled by o.e.m.",
        "none",
        "unknown"
    ];

    public HostInfo? Detect()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS");
            if (key == null) return null;

            string? sysVendor = key.GetValue("SystemManufacturer") as string;
            string? sysProduct = key.GetValue("SystemProductName") as string;
            string? sysVersion = key.GetValue("SystemVersion") as string;
            string? sysFamily = key.GetValue("SystemFamily") as string;

            string? boardVendor = key.GetValue("BaseBoardManufacturer") as string;
            string? boardProduct = key.GetValue("BaseBoardProduct") as string;
            string? boardVersion = key.GetValue("BaseBoardVersion") as string;

            string product = CleanValue(sysProduct);
            string vendor = CleanValue(sysVendor);
            string version = CleanValue(sysVersion);

            if (IsGeneric(product))
            {
                product = CleanValue(boardProduct);
            }

            if (IsGeneric(vendor))
            {
                vendor = CleanValue(boardVendor);
            }

            if (IsGeneric(version))
            {
                version = CleanValue(boardVersion);
            }

            if (string.IsNullOrEmpty(product))
            {
                return null;
            }

            string fullName;
            if (!string.IsNullOrEmpty(vendor) && !product.Contains(vendor, StringComparison.OrdinalIgnoreCase))
            {
                fullName = $"{vendor} {product}";
            }
            else
            {
                fullName = product;
            }

            return new HostInfo
            {
                Name = fullName,
                Vendor = vendor,
                Version = version,
                Family = CleanValue(sysFamily)
            };
        }
        catch
        {
            return null;
        }
    }

    private static string CleanValue(string? val)
    {
        return string.IsNullOrWhiteSpace(val) ? string.Empty : val.Trim();
    }

    private static bool IsGeneric(string val)
    {
        if (string.IsNullOrEmpty(val)) return true;
        string lower = val.ToLowerInvariant();
        return GenericNames.Any(g => lower.Contains(g));
    }
}
