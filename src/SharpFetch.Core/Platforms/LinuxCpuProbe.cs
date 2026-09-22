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

    // ARM Ltd (implementer 0x41) "CPU part" IDs from /proc/cpuinfo, mapped to
    // marketing names - modern arm64 has no "model name"/"Model" field at all
    // (unlike x86 or ARM SBCs with a devicetree), so cloud/server ARM64 hosts
    // (Ampere Altra and similar Neoverse-based CI runners included) need this
    // decode or they report nothing but a raw implementer/part hex pair. Table
    // subset ported from fastfetch's cpu_arm.h armPartId2name, covering
    // Cortex-A/X and Neoverse cores likely to appear on real or CI hardware.
    private static readonly Dictionary<uint, string> ArmPartNames = new()
    {
        [0xd01] = "Cortex-A32",
        [0xd02] = "Cortex-A34",
        [0xd03] = "Cortex-A53",
        [0xd04] = "Cortex-A35",
        [0xd05] = "Cortex-A55",
        [0xd06] = "Cortex-A65",
        [0xd07] = "Cortex-A57",
        [0xd08] = "Cortex-A72",
        [0xd09] = "Cortex-A73",
        [0xd0a] = "Cortex-A75",
        [0xd0b] = "Cortex-A76",
        [0xd0c] = "Neoverse-N1",
        [0xd0d] = "Cortex-A77",
        [0xd0e] = "Cortex-A76AE",
        [0xd40] = "Neoverse-V1",
        [0xd41] = "Cortex-A78",
        [0xd42] = "Cortex-A78AE",
        [0xd44] = "Cortex-X1",
        [0xd47] = "Cortex-A710",
        [0xd48] = "Cortex-X2",
        [0xd49] = "Neoverse-N2",
        [0xd4a] = "Neoverse-E1",
        [0xd4b] = "Cortex-A78C",
        [0xd4c] = "Cortex-X1C",
        [0xd4d] = "Cortex-A715",
        [0xd4e] = "Cortex-X3",
        [0xd4f] = "Neoverse-V2",
        [0xd81] = "Cortex-A720",
        [0xd82] = "Cortex-X4",
        [0xd84] = "Neoverse-V3",
        [0xd85] = "Cortex-X925",
        [0xd87] = "Cortex-A725",
        [0xd8e] = "Neoverse-N3",
    };

    public CpuInfo Detect()
    {
        string model = string.Empty;
        string vendor = string.Empty;
        string rawImplementer = string.Empty;
        uint? partId = null;

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
                    rawImplementer = value;
                    vendor = ArmImplementers.TryGetValue(value, out string? known) ? known : value;
                }
                else if (key == "CPU part" && partId is null)
                {
                    string hex = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
                    if (uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint parsed))
                    {
                        partId = parsed;
                    }
                }

                if (model.Length > 0 && vendor.Length > 0 && partId is not null)
                {
                    break;
                }
            }
        }
        catch
        {
            // Ignore and fall back to the device-tree lookup / defaults below.
        }

        if (model.Length == 0 && rawImplementer.Equals("0x41", StringComparison.OrdinalIgnoreCase) &&
            partId is uint id && ArmPartNames.TryGetValue(id, out string? partName))
        {
            // arm64 has no "model name"/"Model" field at all (unlike x86 or
            // ARM SBCs with a devicetree) - decode the raw implementer/part
            // pair into a marketing name instead, matching fastfetch.
            model = partName;
        }

        if (model.Length == 0)
        {
            // /proc/cpuinfo on many ARM SBCs has no "model name" field at all;
            // the devicetree "model" node (e.g. "Raspberry Pi 4 Model B Rev 1.4")
            // is the standard fallback fastfetch and friends also use.
            model = ReadDeviceTreeModel();
        }

        if (model.Length == 0 && vendor.Length > 0 && partId is uint unmapped)
        {
            // Unknown part on a known implementer: still surface the raw
            // vendor+part pair rather than an uninformative "Unknown CPU".
            model = $"{vendor}-{unmapped:X}";
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
