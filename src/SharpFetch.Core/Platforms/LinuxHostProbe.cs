using System.Runtime.Versioning;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms;

[SupportedOSPlatform("linux")]
public sealed class LinuxHostProbe : IHostProbe
{
    private const string DmiPath = "/sys/class/dmi/id";
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
        // 1. Try DMI sysfs (x86_64, standard ARM servers/laptops)
        if (Directory.Exists(DmiPath))
        {
            string product = ReadTrimmed(Path.Combine(DmiPath, "product_name"));
            string vendor = ReadTrimmed(Path.Combine(DmiPath, "sys_vendor"));
            string version = ReadTrimmed(Path.Combine(DmiPath, "product_version"));
            string family = ReadTrimmed(Path.Combine(DmiPath, "product_family"));

            if (IsGeneric(product))
            {
                product = ReadTrimmed(Path.Combine(DmiPath, "board_name"));
            }

            if (IsGeneric(vendor))
            {
                vendor = ReadTrimmed(Path.Combine(DmiPath, "board_vendor"));
            }

            if (IsGeneric(version))
            {
                version = ReadTrimmed(Path.Combine(DmiPath, "board_version"));
            }

            if (!string.IsNullOrEmpty(product) && !IsGeneric(product))
            {
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
                    Vendor = string.IsNullOrEmpty(vendor) ? null : vendor,
                    Version = string.IsNullOrEmpty(version) ? null : version,
                    Family = string.IsNullOrEmpty(family) ? null : family
                };
            }
        }

        // 2. Try device-tree model (Raspberry Pi, Orange Pi, Rockchip, Apple Silicon Asahi)
        string dtModel = ReadDeviceTree("/proc/device-tree/model");
        if (string.IsNullOrEmpty(dtModel))
        {
            dtModel = ReadDeviceTree("/sys/firmware/devicetree/base/model");
        }

        if (!string.IsNullOrEmpty(dtModel))
        {
            return new HostInfo
            {
                Name = dtModel
            };
        }

        return null;
    }

    private static string ReadDeviceTree(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                // Device-tree model is typically null-terminated
                byte[] bytes = File.ReadAllBytes(path);
                int nullIdx = Array.IndexOf(bytes, (byte)0);
                int len = nullIdx >= 0 ? nullIdx : bytes.Length;
                return System.Text.Encoding.UTF8.GetString(bytes, 0, len).Trim();
            }
        }
        catch
        {
            // Ignore
        }
        return string.Empty;
    }

    private static string ReadTrimmed(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                return File.ReadAllText(path).Trim();
            }
        }
        catch
        {
            // Ignore
        }
        return string.Empty;
    }

    private static bool IsGeneric(string val)
    {
        if (string.IsNullOrEmpty(val)) return true;
        string lower = val.ToLowerInvariant();
        return GenericNames.Any(g => lower.Contains(g));
    }
}
