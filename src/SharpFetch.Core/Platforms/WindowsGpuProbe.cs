using System.Runtime.Versioning;
using Microsoft.Win32;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms;

[SupportedOSPlatform("windows")]
public sealed class WindowsGpuProbe : IGpuProbe
{
    // GUID_DEVCLASS_DISPLAY - the registry class every WDDM display adapter registers under,
    // one numbered subkey ("0000", "0001", ...) per adapter instance. Used only as a fallback
    // if D3DKMT enumeration finds nothing.
    private const string DisplayClassPath = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

    public IReadOnlyList<GpuAdapterInfo> Detect()
    {
        List<GpuAdapterInfo> adapters = DetectViaD3dKmt();
        return adapters.Count > 0 ? adapters : DetectViaRegistry();
    }

    /// <summary>
    /// D3DKMT is the mechanism fastfetch itself uses on Windows (see D3dKmt.cs
    /// for the full rationale and empirical verification against real hardware).
    /// Reports accurate dedicated VRAM and integrated/discrete type, and
    /// correctly excludes software/virtual adapters (e.g. an RDP session's
    /// virtual display) that the registry enumeration below can't distinguish
    /// from a real GPU.
    /// </summary>
    private static List<GpuAdapterInfo> DetectViaD3dKmt()
    {
        var results = new List<GpuAdapterInfo>();

        try
        {
            List<D3dKmt.Adapter> hardwareAdapters = D3dKmt.EnumerateHardwareAdapters();
            int index = 0;
            foreach (D3dKmt.Adapter adapter in hardwareAdapters)
            {
                try
                {
                    if (string.IsNullOrEmpty(adapter.Name))
                    {
                        continue;
                    }

                    long? vram = D3dKmt.TryGetSegmentSize(adapter.Handle, out D3dKmt.SegmentSizeInfo segment)
                        ? (long)segment.DedicatedVideoMemorySize
                        : null;

                    bool? isIntegrated = (adapter.AdapterType & D3dKmt.AdapterTypeHybridIntegrated) != 0 ? true
                        : (adapter.AdapterType & D3dKmt.AdapterTypeHybridDiscrete) != 0 ? false
                        : null;

                    results.Add(new GpuAdapterInfo
                    {
                        Index = index++,
                        Name = adapter.Name,
                        Vendor = string.Empty,
                        DedicatedVramBytes = vram,
                        IsIntegrated = isIntegrated
                    });
                }
                finally
                {
                    D3dKmt.CloseHandle(adapter.Handle);
                }
            }
        }
        catch
        {
            // gdi32's D3DKMT exports are unavailable (unexpected on any
            // supported Windows version, but not worth crashing over) -
            // fall through to the registry-based path.
        }

        return results;
    }

    private static List<GpuAdapterInfo> DetectViaRegistry()
    {
        var adapters = new List<GpuAdapterInfo>();

        try
        {
            using var classKey = Registry.LocalMachine.OpenSubKey(DisplayClassPath);
            if (classKey is null)
            {
                return adapters;
            }

            int index = 0;
            foreach (string subKeyName in classKey.GetSubKeyNames())
            {
                if (subKeyName.Length != 4 || !int.TryParse(subKeyName, out _))
                {
                    continue;
                }

                try
                {
                    using var adapterKey = classKey.OpenSubKey(subKeyName);
                    string? name = adapterKey?.GetValue("DriverDesc") as string;
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    string vendor = adapterKey?.GetValue("ProviderName") as string ?? string.Empty;
                    long? vram = adapterKey?.GetValue("HardwareInformation.qwMemorySize") as long?;

                    adapters.Add(new GpuAdapterInfo
                    {
                        Index = index++,
                        Name = name,
                        Vendor = vendor,
                        DedicatedVramBytes = vram
                    });
                }
                catch
                {
                    // A single adapter subkey can be ACL-restricted in locked-down
                    // environments; skip it and keep enumerating the rest.
                }
            }
        }
        catch
        {
            // Ignore and return whatever was collected so far (possibly empty).
        }

        return adapters;
    }
}
