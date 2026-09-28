using SharpFetch.Core.Models;
using SharpFetch.Core.Modules;
using SharpFetch.Core.Platforms.MacOS;
using SharpFetch.Core.Probes;
using Xunit;

namespace SharpFetch.Tests;

public class BatteryModuleTests
{
    private sealed class MockBatteryProbe(IReadOnlyList<BatteryInfo> batteries) : IBatteryProbe
    {
        public IReadOnlyList<BatteryInfo> DetectBatteries() => batteries;
    }

    [Fact]
    public void Fetch_WhenNoBatteries_ReturnsEmptyList()
    {
        var probe = new MockBatteryProbe([]);
        var module = new BatteryModule(probe);

        var results = module.Fetch();

        Assert.Empty(results);
    }

    [Fact]
    public void Fetch_WithSingleChargingBattery_FormatsCorrectly()
    {
        var probe = new MockBatteryProbe([
            new BatteryInfo
            {
                DeviceName = "Internal",
                CapacityPercent = 85.0,
                State = BatteryState.Charging,
                IsAcConnected = true,
                PowerWatts = 45.2,
                TimeRemaining = new TimeSpan(1, 15, 0)
            }
        ]);
        var module = new BatteryModule(probe);

        var results = module.Fetch();

        Assert.Single(results);
        Assert.Equal("Battery", results[0].DisplayName);
        Assert.Equal("85% [Charging] (45.2W, 1h 15m remaining)", results[0].FormattedValue);
    }

    [Fact]
    public void Fetch_WithFullBattery_FormatsCorrectly()
    {
        var probe = new MockBatteryProbe([
            new BatteryInfo
            {
                DeviceName = "Internal",
                CapacityPercent = 100.0,
                State = BatteryState.Full,
                IsAcConnected = true
            }
        ]);
        var module = new BatteryModule(probe);

        var results = module.Fetch();

        Assert.Single(results);
        Assert.Equal("100% [Full]", results[0].FormattedValue);
    }

    [Fact]
    public void Fetch_WithDischargingBattery_FormatsRemainingTime()
    {
        var probe = new MockBatteryProbe([
            new BatteryInfo
            {
                DeviceName = "Internal",
                CapacityPercent = 42.0,
                State = BatteryState.Discharging,
                IsAcConnected = false,
                TimeRemaining = new TimeSpan(2, 45, 0)
            }
        ]);
        var module = new BatteryModule(probe);

        var results = module.Fetch();

        Assert.Single(results);
        Assert.Equal("42% [Discharging] (2h 45m remaining)", results[0].FormattedValue);
    }

    [Fact]
    public void Fetch_WithMultipleBatteries_FormatsIndividualNames()
    {
        var probe = new MockBatteryProbe([
            new BatteryInfo
            {
                DeviceName = "BAT0",
                CapacityPercent = 90.0,
                State = BatteryState.Full,
                IsAcConnected = true
            },
            new BatteryInfo
            {
                DeviceName = "BAT1",
                CapacityPercent = 65.0,
                State = BatteryState.Charging,
                IsAcConnected = true
            }
        ]);
        var module = new BatteryModule(probe);

        var results = module.Fetch();

        Assert.Equal(2, results.Count);
        Assert.Equal("Battery (BAT0)", results[0].DisplayName);
        Assert.Equal("90% [Full]", results[0].FormattedValue);
        Assert.Equal("Battery (BAT1)", results[1].DisplayName);
        Assert.Equal("65% [Charging]", results[1].FormattedValue);
    }

    [Fact]
    public void MacOsBatteryProbe_ParsePmsetOutput_ParsesDischarging()
    {
        string pmset = """
            Now drawing from 'Battery Power'
             -InternalBattery-0 (id=4849763)	85%; discharging; 3:45 remaining present: true
            """;

        var batteries = MacOsBatteryProbe.ParsePmsetOutput(pmset);

        Assert.Single(batteries);
        var b = batteries[0];
        Assert.Equal("InternalBattery-0", b.DeviceName);
        Assert.Equal(85.0, b.CapacityPercent);
        Assert.Equal(BatteryState.Discharging, b.State);
        Assert.False(b.IsAcConnected);
        Assert.NotNull(b.TimeRemaining);
        Assert.Equal(3, b.TimeRemaining!.Value.Hours);
        Assert.Equal(45, b.TimeRemaining!.Value.Minutes);
    }

    [Fact]
    public void MacOsBatteryProbe_ParsePmsetOutput_DesktopHasNoBattery()
    {
        string pmset = """
            Now drawing from 'AC Power'
            """;

        var batteries = MacOsBatteryProbe.ParsePmsetOutput(pmset);

        Assert.Empty(batteries);
    }
}
