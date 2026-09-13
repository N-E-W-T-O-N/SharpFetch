using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.RegularExpressions;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms;

/// <summary>
/// macOS has no sysctl or virtual-filesystem equivalent for GPU enumeration
/// the way Windows' registry or Linux's PCI sysfs do. A correct probe needs
/// either IOKit/Metal.framework interop - Objective-C message dispatch and
/// CoreFoundation retain/release semantics that are easy to get subtly wrong
/// and cannot be verified without macOS hardware to run against - or the
/// documented `system_profiler` command. This uses the latter: slower
/// (~50-150ms) than the P/Invoke probes on other platforms, but correct and
/// safe rather than an unverified native interop surface.
/// </summary>
[SupportedOSPlatform("macos")]
public sealed partial class MacOsGpuProbe : IGpuProbe
{
    [GeneratedRegex(@"([\d.]+)\s*(GB|MB)", RegexOptions.IgnoreCase)]
    private static partial Regex VramPattern();

    public IReadOnlyList<GpuAdapterInfo> Detect()
    {
        var adapters = new List<GpuAdapterInfo>();

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "/usr/sbin/system_profiler",
                Arguments = "SPDisplaysDataType -json",
                RedirectStandardOutput = true,
                // Not redirecting stderr: it's unread here, and redirecting a
                // stream nobody drains risks a deadlock if the child fills that
                // pipe's OS buffer while this process is blocked reading stdout.
                UseShellExecute = false
            });

            if (process is null)
            {
                return adapters;
            }

            string json = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("SPDisplaysDataType", out JsonElement gpuArray))
            {
                return adapters;
            }

            int index = 0;
            foreach (JsonElement gpu in gpuArray.EnumerateArray())
            {
                // "_name" is Apple's generic display-name key, used consistently
                // across every system_profiler data type, not just displays.
                string name = gpu.TryGetProperty("_name", out JsonElement nameEl)
                    ? nameEl.GetString() ?? "Unknown GPU"
                    : "Unknown GPU";

                string vendor = "Apple";
                if (name.Contains("AMD", StringComparison.OrdinalIgnoreCase) || name.Contains("Radeon", StringComparison.OrdinalIgnoreCase))
                {
                    vendor = "AMD";
                }
                else if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) || name.Contains("GeForce", StringComparison.OrdinalIgnoreCase))
                {
                    vendor = "NVIDIA";
                }
                else if (name.Contains("Intel", StringComparison.OrdinalIgnoreCase))
                {
                    vendor = "Intel";
                }

                adapters.Add(new GpuAdapterInfo
                {
                    Index = index++,
                    Name = name,
                    Vendor = vendor,
                    DedicatedVramBytes = FindVramBytes(gpu)
                });
            }
        }
        catch
        {
            // system_profiler missing/unavailable (non-standard install,
            // sandboxed environment) - report no adapters rather than throwing.
        }

        return adapters;
    }

    /// <summary>
    /// Searches every property for one whose name contains "vram" and whose
    /// value matches a "&lt;number&gt; MB|GB" pattern. system_profiler's exact
    /// key name for this varies by macOS version and GPU type (integrated vs
    /// discrete), so this matches on shape rather than a specific key name.
    /// </summary>
    private static long? FindVramBytes(JsonElement gpu)
    {
        foreach (JsonProperty prop in gpu.EnumerateObject())
        {
            if (!prop.Name.Contains("vram", StringComparison.OrdinalIgnoreCase) ||
                prop.Value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            string? text = prop.Value.GetString();
            if (text is null)
            {
                continue;
            }

            Match match = VramPattern().Match(text);
            if (!match.Success || !double.TryParse(match.Groups[1].Value, out double value))
            {
                continue;
            }

            double multiplier = match.Groups[2].Value.Equals("GB", StringComparison.OrdinalIgnoreCase)
                ? 1024.0 * 1024 * 1024
                : 1024.0 * 1024;

            return (long)(value * multiplier);
        }

        return null;
    }
}
