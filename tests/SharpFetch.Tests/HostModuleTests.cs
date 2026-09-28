using SharpFetch.Core.Models;
using SharpFetch.Core.Modules;
using SharpFetch.Core.Platforms.MacOS;
using SharpFetch.Core.Probes;
using Xunit;

namespace SharpFetch.Tests;

public class HostModuleTests
{
    private sealed class MockHostProbe(HostInfo? info) : IHostProbe
    {
        public HostInfo? Detect() => info;
    }

    [Fact]
    public void Fetch_WhenHostIsNull_ReturnsEmptyList()
    {
        var probe = new MockHostProbe(null);
        var module = new HostModule(probe);

        var results = module.Fetch();

        Assert.Empty(results);
    }

    [Fact]
    public void Fetch_WithHostWithoutVersion_FormatsName()
    {
        var probe = new MockHostProbe(new HostInfo
        {
            Name = "ASUS PRIME B760-PLUS",
            Vendor = "ASUS"
        });
        var module = new HostModule(probe);

        var results = module.Fetch();

        Assert.Single(results);
        Assert.Equal("Host", results[0].DisplayName);
        Assert.Equal("ASUS PRIME B760-PLUS", results[0].FormattedValue);
    }

    [Fact]
    public void Fetch_WithHostAndVersion_FormatsNameAndVersion()
    {
        var probe = new MockHostProbe(new HostInfo
        {
            Name = "Dell XPS 15 9520",
            Version = "1.0",
            Vendor = "Dell Inc."
        });
        var module = new HostModule(probe);

        var results = module.Fetch();

        Assert.Single(results);
        Assert.Equal("Dell XPS 15 9520 (1.0)", results[0].FormattedValue);
    }

    [Theory]
    [InlineData("Mac14,2", "MacBook Air 13\" (M2, 2022)")]
    [InlineData("MacBookPro18,1", "MacBook Pro 16\" (M1 Pro/Max, 2021)")]
    [InlineData("Macmini9,1", "Mac mini (M1, 2020)")]
    public void MacOsHostProbe_ResolveMacModel_ResolvesKnownHardware(string model, string expected)
    {
        string actual = MacOsHostProbe.ResolveMacModel(model);
        Assert.Equal(expected, actual);
    }
}
