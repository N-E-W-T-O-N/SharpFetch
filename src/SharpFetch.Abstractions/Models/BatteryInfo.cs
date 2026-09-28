namespace SharpFetch.Core.Models;

public enum BatteryState
{
    Unknown,
    Charging,
    Discharging,
    Full,
    NotCharging
}

public sealed record BatteryInfo
{
    public required string DeviceName { get; init; }
    public double CapacityPercent { get; init; }
    public BatteryState State { get; init; } = BatteryState.Unknown;
    public bool IsAcConnected { get; init; }
    public string? Manufacturer { get; init; }
    public string? ModelName { get; init; }
    public string? Technology { get; init; }
    public double? PowerWatts { get; init; }
    public TimeSpan? TimeRemaining { get; init; }
    public int? CycleCount { get; init; }
    public double? TemperatureCelsius { get; init; }
}
