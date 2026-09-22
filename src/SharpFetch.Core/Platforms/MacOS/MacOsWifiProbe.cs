using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.RegularExpressions;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Platforms.MacOS;

/// <summary>
/// macOS has no sysctl or virtual-filesystem equivalent for Wi-Fi status.
/// A correct probe needs CoreWLAN - genuine Objective-C (@interface/@property,
/// objc_msgSend message dispatch) plus IOKit registry traversal for the
/// connection frequency - which is exactly the native interop surface
/// MacOsGpuProbe already declined for the same reason: it's easy to get
/// subtly wrong and cannot be verified without macOS hardware to run against.
/// This uses `system_profiler SPAirPortDataType -json` instead: slower, but
/// safe, documented, and consistent with the GPU probe's precedent.
/// </summary>
[SupportedOSPlatform("macos")]
public sealed partial class MacOsWifiProbe : IWifiProbe
{
    public IReadOnlyList<WifiConnectionInfo> DetectWifi()
    {
        var results = new List<WifiConnectionInfo>();

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "/usr/sbin/system_profiler",
                Arguments = "SPAirPortDataType -json",
                RedirectStandardOutput = true,
                // Not redirecting stderr: see MacOsGpuProbe for why redirecting an
                // unread stream risks a deadlock.
                UseShellExecute = false
            });

            if (process is null)
            {
                return results;
            }

            string json = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("SPAirPortDataType", out JsonElement airportArray))
            {
                return results;
            }

            foreach (JsonElement airport in airportArray.EnumerateArray())
            {
                if (!airport.TryGetProperty("spairport_airport_interfaces", out JsonElement interfaces))
                {
                    continue;
                }

                foreach (JsonElement iface in interfaces.EnumerateArray())
                {
                    WifiConnectionInfo? connection = ParseInterface(iface);
                    if (connection is not null)
                    {
                        results.Add(connection);
                    }
                }
            }
        }
        catch
        {
            // system_profiler missing/unavailable - report no connections rather
            // than throwing.
        }

        return results;
    }

    /// <summary>
    /// system_profiler nests the currently-associated network under a key whose
    /// exact name/shape has shifted across macOS versions in public examples
    /// (e.g. "spairport_current_network_information"); this looks for any
    /// property whose value is an object containing a "_name" (the SSID), which
    /// is more resilient to that drift than hardcoding one key name - the same
    /// defensive approach MacOsGpuProbe uses for VRAM.
    /// </summary>
    private static WifiConnectionInfo? ParseInterface(JsonElement iface)
    {
        string? interfaceName = iface.TryGetProperty("_name", out JsonElement nameEl)
            ? nameEl.GetString()
            : null;

        foreach (JsonProperty prop in iface.EnumerateObject())
        {
            if (prop.Value.ValueKind != JsonValueKind.Object ||
                !prop.Value.TryGetProperty("_name", out JsonElement ssidEl))
            {
                continue;
            }

            string? ssid = ssidEl.GetString();
            if (string.IsNullOrEmpty(ssid))
            {
                continue;
            }

            JsonElement network = prop.Value;

            return new WifiConnectionInfo
            {
                InterfaceName = interfaceName ?? "Wi-Fi",
                Ssid = ssid,
                Bssid = GetString(network, "spairport_network_bssid"),
                Protocol = GetDisplayString(network, "spairport_network_phymode"),
                Security = GetDisplayString(network, "spairport_security_mode"),
                Channel = GetChannelNumber(network),
                SignalQualityPercent = GetSignalQualityPercent(network) ?? 0
            };
        }

        return null;
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>
    /// Like <see cref="GetString"/>, but rejects values that look like an
    /// unprocessed internal key (e.g. "spairport_security_mode_wpa3_personal")
    /// rather than display text (e.g. "WPA3 Personal"). system_profiler's exact
    /// output shape for these enum-style fields could not be confirmed without
    /// real macOS hardware - real display strings for protocol/security never
    /// contain underscores, so this is a cheap, safe guard against showing raw
    /// keys to the user rather than an attempt to guess a transform for them.
    /// </summary>
    private static string? GetDisplayString(JsonElement element, string propertyName)
    {
        string? value = GetString(element, propertyName);
        return value is not null && value.Contains('_') ? null : value;
    }

    /// <summary>
    /// Searches for any property whose name contains "signal" and whose value
    /// is a string with a negative dBm reading (e.g. "-52 dBm / -90 dBm" or
    /// "-52"), matching on shape rather than a specific key name for the same
    /// reason as <c>MacOsGpuProbe.FindVramBytes</c>. Converts RSSI to a 0-100
    /// quality percentage using the same formula fastfetch's wifi detection
    /// uses: -50 dBm or better is 100%, -100 dBm or worse is 0%, linear between.
    /// </summary>
    private static int? GetSignalQualityPercent(JsonElement network)
    {
        foreach (JsonProperty prop in network.EnumerateObject())
        {
            if (!prop.Name.Contains("signal", StringComparison.OrdinalIgnoreCase) ||
                prop.Value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            string? text = prop.Value.GetString();
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            Match match = RssiPattern().Match(text);
            if (!match.Success || !int.TryParse(match.Value, out int rssi))
            {
                continue;
            }

            int quality = rssi >= -50 ? 100 : rssi <= -100 ? 0 : (rssi + 100) * 2;
            return Math.Clamp(quality, 0, 100);
        }

        return null;
    }

    [GeneratedRegex(@"-\d+")]
    private static partial Regex RssiPattern();

    /// <summary>
    /// spairport_network_channel is typically a string like "36 (5GHz, 80MHz)";
    /// only the leading channel number is extracted.
    /// </summary>
    private static int? GetChannelNumber(JsonElement network)
    {
        string? raw = GetString(network, "spairport_network_channel");
        if (string.IsNullOrEmpty(raw))
        {
            return null;
        }

        int digits = 0;
        while (digits < raw.Length && char.IsAsciiDigit(raw[digits]))
        {
            digits++;
        }

        return digits > 0 && int.TryParse(raw[..digits], out int channel) ? channel : null;
    }
}
