using SharpFetch.Core.Platforms.Linux;
using Xunit;

namespace SharpFetch.Tests;

public class PciIdsTests
{
    private static readonly string[] SampleDatabase =
    [
        "# pci.ids sample",
        "0001  SafeNet (wrong ID)",
        "10de  NVIDIA Corporation",
        "\t2684  AD102 [GeForce RTX 4090]",
        "\t\t1043 889d  ROG Strix RTX 4090",
        "1d0f  Amazon.com, Inc.",
        "\t8061  NVMe EBS Controller",
        "8086  Intel Corporation",
        "\ta780  Raptor Lake-S GT1 [UHD Graphics 770]",
        "",
        "C 03  Display controller",
        "\t00  VGA compatible controller",
    ];

    [Theory]
    [InlineData("0x10de", "10de")]
    [InlineData("0x10DE", "10de")]
    [InlineData("8086", "8086")]
    [InlineData("", null)]
    [InlineData("0x", null)]
    [InlineData("0x10de0", null)]
    [InlineData("zzzz", null)]
    public void Normalize_HandlesSysfsIds(string input, string? expected)
    {
        Assert.Equal(expected, PciIds.Normalize(input));
    }

    [Fact]
    public void Parse_ResolvesSeveralDevicesInOnePass()
    {
        var result = PciIds.Parse(SampleDatabase, [("10de", "2684"), ("8086", "a780")]);

        Assert.Equal(("NVIDIA Corporation", "AD102 [GeForce RTX 4090]"), result[("10de", "2684")]);
        Assert.Equal(("Intel Corporation", "Raptor Lake-S GT1 [UHD Graphics 770]"), result[("8086", "a780")]);
    }

    [Fact]
    public void Parse_UnknownDeviceOfKnownVendor_ReturnsVendorOnly()
    {
        var result = PciIds.Parse(SampleDatabase, [("1d0f", "1111")]);

        Assert.Equal(("Amazon.com, Inc.", (string?)null), result[("1d0f", "1111")]);
    }

    [Fact]
    public void Parse_SubsystemLineIsNotMistakenForDevice()
    {
        // "1043" only appears as a subsystem vendor ("\t\t1043 889d ...").
        var result = PciIds.Parse(SampleDatabase, [("10de", "1043")]);

        Assert.Equal(("NVIDIA Corporation", (string?)null), result[("10de", "1043")]);
    }

    [Fact]
    public void Parse_UnknownVendor_IsAbsent()
    {
        var result = PciIds.Parse(SampleDatabase, [("abcd", "0001")]);

        Assert.Empty(result);
    }

    [Fact]
    public void Parse_StopsAtClassSection()
    {
        // "C 03" must not be treated as vendor "C 03".
        var result = PciIds.Parse(SampleDatabase, [("C 03", "0000")]);

        Assert.Empty(result);
    }
}
