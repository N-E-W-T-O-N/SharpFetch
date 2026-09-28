using System.Globalization;
using System.Runtime.Versioning;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms;

[SupportedOSPlatform("linux")]
public sealed class LinuxBatteryProbe : IBatteryProbe
{
    private const string PowerSupplyPath = "/sys/class/power_supply";

    public IReadOnlyList<BatteryInfo> DetectBatteries()
    {
        if (!Directory.Exists(PowerSupplyPath))
        {
            return [];
        }

        bool isAcConnected = CheckAcOnline();
        var batteries = new List<BatteryInfo>();

        try
        {
            foreach (var dir in Directory.EnumerateDirectories(PowerSupplyPath))
            {
                string dirName = Path.GetFileName(dir);
                string type = ReadTrimmed(Path.Combine(dir, "type"));

                if (!string.Equals(type, "Battery", StringComparison.OrdinalIgnoreCase) &&
                    !dirName.StartsWith("BAT", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Check scope - some peripherals (mice, keyboards) appear in power_supply with scope=Device
                string scope = ReadTrimmed(Path.Combine(dir, "scope"));
                if (string.Equals(scope, "Device", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Read capacity
                if (!double.TryParse(ReadTrimmed(Path.Combine(dir, "capacity")), NumberStyles.Float, CultureInfo.InvariantCulture, out double capacity))
                {
                    continue;
                }

                // Status
                string rawStatus = ReadTrimmed(Path.Combine(dir, "status"));
                BatteryState state = rawStatus.ToLowerInvariant() switch
                {
                    "charging" => BatteryState.Charging,
                    "discharging" => BatteryState.Discharging,
                    "full" => BatteryState.Full,
                    "not charging" => BatteryState.NotCharging,
                    _ => BatteryState.Unknown
                };

                // Power in Watts
                double? powerWatts = null;
                if (long.TryParse(ReadTrimmed(Path.Combine(dir, "power_now")), out long powerUWatts) && powerUWatts > 0)
                {
                    powerWatts = powerUWatts / 1_000_000.0;
                }
                else if (long.TryParse(ReadTrimmed(Path.Combine(dir, "current_now")), out long currentUAmps) &&
                         long.TryParse(ReadTrimmed(Path.Combine(dir, "voltage_now")), out long voltageUVolts) &&
                         currentUAmps > 0 && voltageUVolts > 0)
                {
                    powerWatts = (currentUAmps / 1_000_000.0) * (voltageUVolts / 1_000_000.0);
                }

                // Time remaining
                TimeSpan? timeRemaining = null;
                if (powerWatts is > 0)
                {
                    if (state == BatteryState.Discharging &&
                        long.TryParse(ReadTrimmed(Path.Combine(dir, "energy_now")), out long energyNowUWh) &&
                        long.TryParse(ReadTrimmed(Path.Combine(dir, "power_now")), out long powerNowUWh) &&
                        powerNowUWh > 0)
                    {
                        double hours = (double)energyNowUWh / powerNowUWh;
                        if (hours is >= 0 and <= 100)
                        {
                            timeRemaining = TimeSpan.FromHours(hours);
                        }
                    }
                    else if (state == BatteryState.Charging &&
                             long.TryParse(ReadTrimmed(Path.Combine(dir, "energy_full")), out long energyFull) &&
                             long.TryParse(ReadTrimmed(Path.Combine(dir, "energy_now")), out long energyCur) &&
                             long.TryParse(ReadTrimmed(Path.Combine(dir, "power_now")), out long powerCur) &&
                             energyFull > energyCur && powerCur > 0)
                    {
                        double hours = (double)(energyFull - energyCur) / powerCur;
                        if (hours is >= 0 and <= 100)
                        {
                            timeRemaining = TimeSpan.FromHours(hours);
                        }
                    }
                }

                // Cycle count
                int? cycleCount = null;
                if (int.TryParse(ReadTrimmed(Path.Combine(dir, "cycle_count")), out int cycles) && cycles >= 0)
                {
                    cycleCount = cycles;
                }

                // Temperature
                double? tempCelsius = null;
                if (double.TryParse(ReadTrimmed(Path.Combine(dir, "temp")), NumberStyles.Float, CultureInfo.InvariantCulture, out double tempRaw) && tempRaw > 0)
                {
                    tempCelsius = tempRaw / 10.0;
                }

                string manufacturer = ReadTrimmed(Path.Combine(dir, "manufacturer"));
                string modelName = ReadTrimmed(Path.Combine(dir, "model_name"));
                string tech = ReadTrimmed(Path.Combine(dir, "technology"));

                batteries.Add(new BatteryInfo
                {
                    DeviceName = dirName,
                    CapacityPercent = Math.Clamp(capacity, 0.0, 100.0),
                    State = state,
                    IsAcConnected = isAcConnected,
                    Manufacturer = string.IsNullOrEmpty(manufacturer) ? null : manufacturer,
                    ModelName = string.IsNullOrEmpty(modelName) ? null : modelName,
                    Technology = string.IsNullOrEmpty(tech) ? null : tech,
                    PowerWatts = powerWatts,
                    TimeRemaining = timeRemaining,
                    CycleCount = cycleCount,
                    TemperatureCelsius = tempCelsius
                });
            }
        }
        catch
        {
            // Suppress filesystem/permission issues
        }

        return batteries;
    }

    private static bool CheckAcOnline()
    {
        try
        {
            if (!Directory.Exists(PowerSupplyPath)) return false;

            foreach (var dir in Directory.EnumerateDirectories(PowerSupplyPath))
            {
                string type = ReadTrimmed(Path.Combine(dir, "type"));
                if (string.Equals(type, "Mains", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(type, "USB", StringComparison.OrdinalIgnoreCase) ||
                    Path.GetFileName(dir).StartsWith("AC", StringComparison.OrdinalIgnoreCase))
                {
                    string online = ReadTrimmed(Path.Combine(dir, "online"));
                    if (online == "1")
                    {
                        return true;
                    }
                }
            }
        }
        catch
        {
            // Ignore
        }

        return false;
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
            // Access denied / virtual file transient error
        }

        return string.Empty;
    }
}
