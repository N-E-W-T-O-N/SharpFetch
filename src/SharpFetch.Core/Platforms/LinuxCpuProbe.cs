using System.Globalization;
using System.Runtime.Versioning;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms;

[SupportedOSPlatform("linux")]
public sealed class LinuxCpuProbe : ICpuProbe
{
    // Common ARM "CPU implementer" IDs from /proc/cpuinfo, which reports a raw
    // hex code rather than a name on architectures without x86's vendor_id.
    private static readonly Dictionary<string, string> ArmImplementers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["0x41"] = "ARM",
        ["0x42"] = "Broadcom",
        ["0x43"] = "Cavium",
        ["0x4e"] = "NVIDIA",
        ["0x50"] = "Ampere",
        ["0x51"] = "Qualcomm",
        ["0x53"] = "Samsung",
        ["0x56"] = "Marvell",
        ["0x61"] = "Apple",
        ["0x69"] = "Intel",
    };

    public CpuInfo Detect()
    {
        string model = string.Empty;
        string vendor = string.Empty;

        try
        {
            foreach (string line in File.ReadLines("/proc/cpuinfo"))
            {
                int sep = line.IndexOf(':');
                if (sep <= 0) continue;

                string key = line[..sep].Trim();
                string value = line[(sep + 1)..].Trim();
                if (value.Length == 0) continue;

                bool isModelKey = key is "model name" or "Model";
                bool isVendorKey = key is "vendor_id" or "Vendor" or "CPU implementer";

                if (isModelKey && model.Length == 0)
                {
                    model = value;
                }
                else if (isVendorKey && vendor.Length == 0)
                {
                    vendor = ArmImplementers.TryGetValue(value, out string? known) ? known : value;
                }

                if (model.Length > 0 && vendor.Length > 0)
                {
                    break;
                }
            }
        }
        catch
        {
            // Ignore and fall back to the device-tree lookup / defaults below.
        }

        if (model.Length == 0)
        {
            // /proc/cpuinfo on many ARM SBCs has no "model name" field at all;
            // the devicetree "model" node (e.g. "Raspberry Pi 4 Model B Rev 1.4")
            // is the standard fallback fastfetch and friends also use.
            model = ReadDeviceTreeModel();
        }

        if (model.Length == 0)
        {
            model = "Unknown CPU";
        }

        int baseClockMHz = GetBaseClockMHz();

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
    /// Reads SMBIOS Type 4 MaxSpeed from the kernel's exposed raw table, the same
    /// field fastfetch and most fetch tools display as CPU clock speed. See
    /// WindowsCpuProbe.GetMaxClockMHz for why this differs from BaseClockMHz.
    /// </summary>
    private static int GetMaxClockMHz(int baseClockMHz)
    {
        try
        {
            const string dmiTablePath = "/sys/firmware/dmi/tables/DMI";
            if (!File.Exists(dmiTablePath))
            {
                return 0;
            }

            byte[] table = File.ReadAllBytes(dmiTablePath);
            return SmbiosTableParser.FindProcessorMaxSpeedMHz(table, baseClockMHz);
        }
        catch
        {
            // Requires root on some distros/kernel configs; not available in
            // containers without /sys/firmware mounted. Fall back to base clock.
            return 0;
        }
    }

    private static string ReadDeviceTreeModel()
    {
        foreach (string path in new[] { "/proc/device-tree/model", "/sys/firmware/devicetree/base/model" })
        {
            try
            {
                if (File.Exists(path))
                {
                    // devicetree strings are NUL-terminated; trim the trailing byte.
                    return File.ReadAllText(path).TrimEnd('\0').Trim();
                }
            }
            catch
            {
                // Try the next path.
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Counts distinct (physical_package_id, core_id) pairs across
    /// /sys/devices/system/cpu/cpu*/topology - the standard cross-architecture
    /// way to get physical core count. /proc/cpuinfo's "cpu cores" field is
    /// per-socket and unreliable on ARM boards that omit "physical id" entirely.
    /// </summary>
    private static int GetPhysicalCoreCount()
    {
        try
        {
            var cores = new HashSet<(int Package, int Core)>();
            foreach (string cpuDir in Directory.EnumerateDirectories("/sys/devices/system/cpu", "cpu*"))
            {
                // Directory.EnumerateDirectories only supports '*'/'?' wildcards, not
                // POSIX character classes - filter to numbered cpuN dirs by hand to
                // exclude siblings like "cpufreq" and "cpuidle" that also start with "cpu".
                string suffix = Path.GetFileName(cpuDir)["cpu".Length..];
                if (suffix.Length == 0 || !suffix.All(char.IsAsciiDigit))
                {
                    continue;
                }

                string topologyDir = Path.Combine(cpuDir, "topology");
                int package = ReadIntOrDefault(Path.Combine(topologyDir, "physical_package_id"), 0);
                int core = ReadIntOrDefault(Path.Combine(topologyDir, "core_id"), -1);
                if (core >= 0)
                {
                    cores.Add((package, core));
                }
            }

            if (cores.Count > 0)
            {
                return cores.Count;
            }
        }
        catch
        {
            // Fall through to the logical count below.
        }

        return Environment.ProcessorCount;
    }

    private static int GetBaseClockMHz()
    {
        // scaling_max_freq (kHz) is the closest widely-available approximation of
        // a "base" clock across cpufreq governors; there is no universal exact
        // equivalent to Windows' registry ~MHz value on Linux.
        int kHz = ReadIntOrDefault("/sys/devices/system/cpu/cpu0/cpufreq/scaling_max_freq", 0);
        if (kHz > 0)
        {
            return kHz / 1000;
        }

        // Fall back to /proc/cpuinfo's live "cpu MHz" reading when cpufreq is
        // unavailable (e.g. some containers/VMs).
        try
        {
            foreach (string line in File.ReadLines("/proc/cpuinfo"))
            {
                int sep = line.IndexOf(':');
                if (sep <= 0 || line[..sep].Trim() != "cpu MHz") continue;

                if (double.TryParse(line[(sep + 1)..].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double mhz))
                {
                    return (int)Math.Round(mhz);
                }
                break;
            }
        }
        catch
        {
            // Ignore
        }

        return 0;
    }

    private static int ReadIntOrDefault(string path, int fallback)
    {
        try
        {
            if (File.Exists(path) &&
                int.TryParse(File.ReadAllText(path).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                return value;
            }
        }
        catch
        {
            // Ignore
        }

        return fallback;
    }
}
