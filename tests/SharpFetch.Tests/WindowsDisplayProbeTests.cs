using SharpFetch.Core.Platforms.Windows;
using Xunit;

namespace SharpFetch.Tests;

public class WindowsDisplayProbeTests
{
    [Fact]
    public void DevicePathToRegistryKey_MonitorInterfacePath_MapsToEnumKey()
    {
        string? key = WindowsDisplayProbe.DevicePathToRegistryKey(
            @"\\?\DISPLAY#DELA0A5#5&2e0a1c4&0&UID4352#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}");

        Assert.Equal(@"SYSTEM\CurrentControlSet\Enum\DISPLAY\DELA0A5\5&2e0a1c4&0&UID4352\Device Parameters", key);
    }

    [Fact]
    public void DevicePathToRegistryKey_LaptopPanelPath_MapsToEnumKey()
    {
        string? key = WindowsDisplayProbe.DevicePathToRegistryKey(
            @"\\?\DISPLAY#BOE0A1C#4&1a2b3c4d&0&UID8388688#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}");

        Assert.Equal(@"SYSTEM\CurrentControlSet\Enum\DISPLAY\BOE0A1C\4&1a2b3c4d&0&UID8388688\Device Parameters", key);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(@"\\?\USB#VID_1234#xyz#{guid}")]
    [InlineData(@"\\?\DISPLAY#DEL\..\..#x#{guid}")]
    [InlineData(@"\\?\DISPLAY##x#{guid}")]
    public void DevicePathToRegistryKey_UnexpectedPath_ReturnsNull(string? devicePath)
    {
        Assert.Null(WindowsDisplayProbe.DevicePathToRegistryKey(devicePath));
    }

    [Theory]
    [InlineData(60000u, 1000u, 60.0)]
    [InlineData(143981u, 1000u, 143.98)]
    [InlineData(144u, 1u, 144.0)]
    [InlineData(60u, 0u, 0.0)]
    public void ToHz_ConvertsRational(uint numerator, uint denominator, double expected)
    {
        Assert.Equal(expected, WindowsDisplayProbe.ToHz(numerator, denominator));
    }
}
