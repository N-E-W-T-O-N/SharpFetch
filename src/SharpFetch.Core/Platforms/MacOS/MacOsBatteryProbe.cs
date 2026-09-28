using System.Diagnostics;
using System.Globalization;
using System.Runtime.Versioning;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms.MacOS;

public sealed class MacOsBatteryProbe : IBatteryProbe
{
    [SupportedOSPlatform("macos")]
    public IReadOnlyList<BatteryInfo> DetectBatteries()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "/usr/bin/pmset",
                Arguments = "-g batt",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return [];

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(500);

            return ParsePmsetOutput(output);
        }
        catch
        {
            return [];
        }
    }

    public static List<BatteryInfo> ParsePmsetOutput(string output)
    {
        var list = new List<BatteryInfo>();
        if (string.IsNullOrWhiteSpace(output)) return list;

        bool isAc = output.Contains("AC Power", StringComparison.OrdinalIgnoreCase);
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines)
        {
            // Expected line: " -InternalBattery-0 (id=4849763)\t98%; charging; 0:15 remaining present: true"
            if (!line.Contains('%')) continue;

            int tabOrSpace = line.IndexOfAny(['\t', ';']);
            if (tabOrSpace < 0) continue;

            string namePart = line[..tabOrSpace].TrimStart('-', ' ').Trim();
            int idIndex = namePart.IndexOf('(');
            string deviceName = idIndex > 0 ? namePart[..idIndex].Trim() : namePart;
            if (string.IsNullOrEmpty(deviceName)) deviceName = "InternalBattery";

            // Extract percent
            int percentIndex = line.IndexOf('%');
            int numStart = percentIndex - 1;
            while (numStart >= 0 && (char.IsDigit(line[numStart]) || line[numStart] == '.'))
            {
                numStart--;
            }
            numStart++;

            string percentStr = line[numStart..percentIndex];
            if (!double.TryParse(percentStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double capacity))
            {
                continue;
            }

            string lower = line.ToLowerInvariant();
            BatteryState state;
            if (lower.Contains("discharging"))
            {
                state = BatteryState.Discharging;
            }
            else if (lower.Contains("charging") && !lower.Contains("not charging"))
            {
                state = BatteryState.Charging;
            }
            else if (lower.Contains("charged") || (capacity >= 100 && isAc))
            {
                state = BatteryState.Full;
            }
            else if (lower.Contains("not charging") || isAc)
            {
                state = BatteryState.NotCharging;
            }
            else
            {
                state = BatteryState.Unknown;
            }

            // Time remaining (e.g., "3:45 remaining")
            TimeSpan? timeRemaining = null;
            int remIdx = lower.IndexOf("remaining", StringComparison.Ordinal);
            if (remIdx > 0)
            {
                string beforeRem = lower[..remIdx].Trim();
                int lastSemi = beforeRem.LastIndexOf(';');
                string timeStr = (lastSemi >= 0 ? beforeRem[(lastSemi + 1)..] : beforeRem).Trim();

                var parts = timeStr.Split(':');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int hrs) &&
                    int.TryParse(parts[1], out int mins))
                {
                    timeRemaining = new TimeSpan(hrs, mins, 0);
                }
            }

            list.Add(new BatteryInfo
            {
                DeviceName = deviceName,
                CapacityPercent = Math.Clamp(capacity, 0.0, 100.0),
                State = state,
                IsAcConnected = isAc,
                Manufacturer = "Apple",
                TimeRemaining = timeRemaining
            });
        }

        return list;
    }
}
