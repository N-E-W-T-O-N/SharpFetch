using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.RegularExpressions;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Platforms.MacOS;

/// <summary>
/// macOS has no sysctl or virtual-filesystem equivalent for display enumeration
/// the way Windows' EnumDisplayMonitors/DisplayConfig or Linux's /sys/class/drm
/// do. A correct probe needs CoreGraphics (CGGetActiveDisplayList) plus IOKit -
/// genuine Objective-C interop that is easy to get subtly wrong and cannot be
/// verified without macOS hardware to run against, exactly as MacOsGpuProbe and
/// MacOsWifiProbe already decided for their own domains. This uses the same
/// `system_profiler SPDisplaysDataType -json` command the GPU probe already
/// calls: each GPU entry nests its connected displays under an array (key name
/// has shifted across macOS versions in public examples, e.g.
/// "spdisplays_ndrvs"), so this searches for that shape instead of hardcoding
/// one key name - matching FindVramBytes's defensive approach in MacOsGpuProbe.
/// </summary>
[SupportedOSPlatform("macos")]
public sealed partial class MacOsDisplayProbe : IDisplayProbe
{
    [GeneratedRegex(@"(\d+)\s*x\s*(\d+)")]
    private static partial Regex ResolutionPattern();

    [GeneratedRegex(@"([\d.]+)\s*Hz", RegexOptions.IgnoreCase)]
    private static partial Regex RefreshRatePattern();

    public IReadOnlyList<DisplayInfo> DetectDisplays()
    {
        var results = new List<DisplayInfo>();

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "/usr/sbin/system_profiler",
                Arguments = "SPDisplaysDataType -json",
                RedirectStandardOutput = true,
                // Not redirecting stderr: see MacOsGpuProbe for why redirecting
                // an unread stream risks a deadlock.
                UseShellExecute = false
            });

            if (process is null)
            {
                return results;
            }

            string json = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("SPDisplaysDataType", out JsonElement gpuArray))
            {
                return results;
            }

            foreach (JsonElement gpu in gpuArray.EnumerateArray())
            {
                foreach (JsonProperty prop in gpu.EnumerateObject())
                {
                    if (prop.Value.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (JsonElement display in prop.Value.EnumerateArray())
                    {
                        DisplayInfo? info = ParseDisplay(display, results.Count == 0);
                        if (info is not null)
                        {
                            results.Add(info);
                        }
                    }
                }
            }
        }
        catch
        {
            // system_profiler missing/unavailable - report no displays rather
            // than throwing.
        }

        return results;
    }

    private static DisplayInfo? ParseDisplay(JsonElement display, bool isFirst)
    {
        string? name = display.TryGetProperty("_name", out JsonElement nameEl)
            ? nameEl.GetString()
            : null;

        (int width, int height, double refreshRate) = FindResolution(display);
        if (width <= 0 || height <= 0)
        {
            return null;
        }

        bool isPrimary = AnyPropertyEquals(display, "spdisplays_main", "spdisplays_yes") || isFirst;
        bool isBuiltin = (name is not null && name.Contains("built-in", StringComparison.OrdinalIgnoreCase)) ||
                         AnyPropertyContains(display, "built-in");

        return new DisplayInfo
        {
            Name = name ?? "Display",
            Width = width,
            Height = height,
            RefreshRate = refreshRate,
            Type = isBuiltin ? DisplayType.Builtin : DisplayType.External,
            IsPrimary = isPrimary
        };
    }

    /// <summary>
    /// system_profiler reports resolution as a single string like
    /// "1920 x 1080 @ 60.00Hz" or "1920 x 1080" under a key whose exact name
    /// varies by macOS version - searched by shape rather than a fixed key,
    /// same as MacOsGpuProbe.FindVramBytes.
    /// </summary>
    private static (int Width, int Height, double RefreshRate) FindResolution(JsonElement display)
    {
        foreach (JsonProperty prop in display.EnumerateObject())
        {
            if (prop.Value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            string? text = prop.Value.GetString();
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            Match resMatch = ResolutionPattern().Match(text);
            if (!resMatch.Success ||
                !int.TryParse(resMatch.Groups[1].Value, out int width) ||
                !int.TryParse(resMatch.Groups[2].Value, out int height))
            {
                continue;
            }

            double refreshRate = 60.0;
            Match hzMatch = RefreshRatePattern().Match(text);
            if (hzMatch.Success && double.TryParse(hzMatch.Groups[1].Value, out double hz))
            {
                refreshRate = hz;
            }

            return (width, height, refreshRate);
        }

        return (0, 0, 0);
    }

    private static bool AnyPropertyEquals(JsonElement element, string propertyName, string value) =>
        element.TryGetProperty(propertyName, out JsonElement prop) &&
        prop.ValueKind == JsonValueKind.String &&
        string.Equals(prop.GetString(), value, StringComparison.OrdinalIgnoreCase);

    private static bool AnyPropertyContains(JsonElement element, string substring)
    {
        foreach (JsonProperty prop in element.EnumerateObject())
        {
            if (prop.Value.ValueKind == JsonValueKind.String &&
                prop.Value.GetString()?.Contains(substring, StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }
        }

        return false;
    }
}
