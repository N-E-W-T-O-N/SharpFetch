using System.Runtime.Versioning;
using SharpFetch.Core.Models;
using SharpFetch.Core.Platforms.Linux;
using Xunit;

namespace SharpFetch.Tests;

// The helpers under test are pure; the attribute only satisfies the platform
// analyzer for the Linux-annotated classes that host them.
[SupportedOSPlatform("linux")]
public class LinuxDisplayProbeTests
{
    private static DisplayInfo Display(string name) => new() { Name = name, Width = 1920, Height = 1080 };

    [Fact]
    public void Choose_NoX11_UsesDrm()
    {
        var drm = new[] { Display("eDP-1") };

        Assert.Same(drm, LinuxDisplayProbe.Choose(null, drm));
        Assert.Same(drm, LinuxDisplayProbe.Choose([], drm));
    }

    [Fact]
    public void Choose_RealX11Outputs_PreferX11EvenWhenDrmHasAsMany()
    {
        // Laptop panel + monitor: X11 has the current mode and refresh rate.
        var x11 = new[] { Display("BOE 0x0A1C"), Display("DELL U2720Q") };
        var drm = new[] { Display("eDP-1"), Display("HDMI-A-1") };

        Assert.Same(x11, LinuxDisplayProbe.Choose(x11, drm));
    }

    [Fact]
    public void Choose_X11SeesMoreThanDrm_UsesX11()
    {
        // NVIDIA without nvidia-drm.modeset: its HDMI port has no DRM connector.
        var x11 = new[] { Display("eDP-1"), Display("HDMI-0") };
        var drm = new[] { Display("eDP-1") };

        Assert.Same(x11, LinuxDisplayProbe.Choose(x11, drm));
    }

    [Fact]
    public void Choose_XWaylandWithCompleteDrm_UsesDrm()
    {
        var x11 = new[] { Display("XWAYLAND0"), Display("XWAYLAND1") };
        var drm = new[] { Display("eDP-1"), Display("DP-2") };

        Assert.Same(drm, LinuxDisplayProbe.Choose(x11, drm));
    }

    [Fact]
    public void Choose_XWaylandSeesMoreThanDrm_UsesX11()
    {
        var x11 = new[] { Display("XWAYLAND0"), Display("XWAYLAND1") };
        var drm = new[] { Display("eDP-1") };

        Assert.Same(x11, LinuxDisplayProbe.Choose(x11, drm));
    }

    [Theory]
    [InlineData(148500000u, 2200u, 1125u, 0ul, 60.0)]      // 1920x1080@60
    [InlineData(533250000u, 2080u, 1111u, 0ul, 230.76)]    // high-refresh CVT-RB mode
    [InlineData(74250000u, 2200u, 1125u, 0x10ul, 60.0)]    // 1080i: interlace halves vTotal
    [InlineData(25175000u, 800u, 525u, 0x20ul, 29.97)]     // doublescan doubles vTotal
    [InlineData(0u, 0u, 0u, 0ul, 0.0)]                     // Xvfb's mode has no timings
    public void RefreshRate_MatchesXrandrFormula(uint dotClock, uint hTotal, uint vTotal, ulong flags, double expected)
    {
        Assert.Equal(expected, X11Displays.RefreshRate(dotClock, hTotal, vTotal, flags));
    }

    [Theory]
    [InlineData("eDP-1", DisplayType.Builtin)]
    [InlineData("eDP1", DisplayType.Builtin)]
    [InlineData("LVDS-1", DisplayType.Builtin)]
    [InlineData("DSI-1", DisplayType.Builtin)]
    [InlineData("HDMI-1", DisplayType.External)]
    [InlineData("DP-2", DisplayType.External)]
    [InlineData(null, DisplayType.Unknown)]
    public void ClassifyOutput_ByConnectorName(string? outputName, DisplayType expected)
    {
        Assert.Equal(expected, X11Displays.ClassifyOutput(outputName));
    }

    [Theory]
    [InlineData(597, 336, 27)]
    [InlineData(344, 194, 16)]
    [InlineData(0, 0, null)]
    public void DiagonalInches_FromMillimetres(int widthMm, int heightMm, int? expected)
    {
        Assert.Equal(expected, X11Displays.DiagonalInches(widthMm, heightMm));
    }
}
