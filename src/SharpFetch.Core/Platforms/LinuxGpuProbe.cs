using System.Globalization;
using System.Runtime.Versioning;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms;

[SupportedOSPlatform("linux")]
public sealed class LinuxGpuProbe : IGpuProbe
{
    // PCI class 0x03xxxx covers Display controllers (VGA 0x0300, XGA 0x0301,
    // 3D 0x0302, Other display 0x0380).
    private const string DisplayClassPrefix = "0x03";

    // A small, stable set of well-known PCI vendor IDs for a friendlier name.
    // Full model-name resolution needs a pci.ids database (thousands of
    // vendor:device entries, e.g. fastfetch's ENABLE_EMBEDDED_PCIIDS) which
    // isn't bundled here - unrecognized vendors/devices report their raw PCI
    // IDs rather than a guessed name.
    private static readonly Dictionary<string, string> KnownVendors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["0x10de"] = "NVIDIA",
        ["0x1002"] = "AMD",
        ["0x8086"] = "Intel",
        ["0x1af4"] = "Red Hat (virtio-gpu)",
        ["0x15ad"] = "VMware",
        ["0x1414"] = "Microsoft (Hyper-V)",
        ["0x80ee"] = "VirtualBox",
    };

    public IReadOnlyList<GpuAdapterInfo> Detect()
    {
        var adapters = new List<GpuAdapterInfo>();

        try
        {
            int index = 0;
            foreach (string deviceDir in Directory.EnumerateDirectories("/sys/bus/pci/devices"))
            {
                try
                {
                    string classValue = ReadTrimmed(Path.Combine(deviceDir, "class"));
                    if (!classValue.StartsWith(DisplayClassPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string vendorId = ReadTrimmed(Path.Combine(deviceDir, "vendor"));
                    string deviceId = ReadTrimmed(Path.Combine(deviceDir, "device"));
                    string vendorName = KnownVendors.TryGetValue(vendorId, out string? known) ? known : vendorId;

                    adapters.Add(new GpuAdapterInfo
                    {
                        Index = index++,
                        Name = $"{vendorName} {deviceId}".Trim(),
                        Vendor = vendorName,
                        DedicatedVramBytes = ReadAmdgpuVram(deviceDir)
                    });
                }
                catch
                {
                    // A single device directory can disappear mid-enumeration
                    // (hot-unplug) or be unreadable; skip it and keep going.
                }
            }
        }
        catch
        {
            // Ignore /sys/bus/pci read errors (e.g. non-PCI platforms)
        }

        if (adapters.Count == 0)
        {
            DetectSocGpu(adapters);
        }

        return adapters;
    }

    private static void DetectSocGpu(List<GpuAdapterInfo> adapters)
    {
        if (!Directory.Exists("/sys/class/drm")) return;

        try
        {
            foreach (string cardDir in Directory.EnumerateDirectories("/sys/class/drm", "card[0-9]*"))
            {
                if (Path.GetFileName(cardDir).Contains('-')) continue;

                string driverLink = Path.Combine(cardDir, "device", "driver");
                string driverName = string.Empty;
                if (Directory.Exists(driverLink))
                {
                    try
                    {
                        var target = Directory.ResolveLinkTarget(driverLink, false);
                        driverName = target != null ? Path.GetFileName(target.FullName) : string.Empty;
                    }
                    catch
                    {
                        // Ignore symlink resolution failures
                    }
                }

                if (string.IsNullOrEmpty(driverName)) continue;

                string gpuName = driverName switch
                {
                    "vc4" or "v3d" => "Broadcom VideoCore (Raspberry Pi)",
                    "panfrost" or "mali" => "ARM Mali GPU",
                    "lima" => "ARM Mali (Utgard)",
                    "msm" or "kgsl-3d0" => "Qualcomm Adreno",
                    "etnaviv" => "Vivante GPU",
                    "tegra" or "tegra-drm" => "NVIDIA Tegra",
                    "virtio-gpu" or "virtio_gpu" => "Red Hat VirtIO GPU",
                    "bochs-drm" => "Bochs Display Adapter",
                    "simple-framebuffer" or "simpledrm" => "Simple DRM / EFI Framebuffer",
                    _ => $"{driverName} DRM"
                };

                string vendor = driverName is "vc4" or "v3d" ? "Broadcom" :
                                driverName is "panfrost" or "lima" or "mali" ? "ARM" :
                                driverName is "msm" or "kgsl-3d0" ? "Qualcomm" : string.Empty;

                adapters.Add(new GpuAdapterInfo
                {
                    Index = adapters.Count,
                    Name = gpuName,
                    Vendor = vendor,
                    IsIntegrated = true
                });
                break;
            }
        }
        catch
        {
            // Ignore
        }
    }

    /// <summary>
    /// mem_info_vram_total is exposed only by the open-source amdgpu driver's
    /// DRM sysfs node. NVIDIA's proprietary driver and Intel's i915 publish no
    /// equivalent file without vendor tooling, so this returns null for those.
    /// </summary>
    private static long? ReadAmdgpuVram(string pciDeviceDir)
    {
        string vramFile = Path.Combine(pciDeviceDir, "mem_info_vram_total");
        return File.Exists(vramFile) &&
               long.TryParse(ReadTrimmed(vramFile), NumberStyles.Integer, CultureInfo.InvariantCulture, out long bytes)
            ? bytes
            : null;
    }

    private static string ReadTrimmed(string path) => File.Exists(path) ? File.ReadAllText(path).Trim() : string.Empty;
}
