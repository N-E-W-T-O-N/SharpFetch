using SharpFetch.Core.Models;
using SharpFetch.UI;
using Spectre.Console;
using Xunit;

namespace SharpFetch.Tests;

public class AsciiArtTests
{
    public static TheoryData<OsFamily, string> AllOsFamilies =>
        new()
        {
            { OsFamily.Windows, "Windows 11" },
            { OsFamily.Linux, "Arch Linux" },
            { OsFamily.Linux, "Ubuntu 24.04" },
            { OsFamily.Linux, "Debian GNU/Linux" },
            { OsFamily.Linux, "Gentoo Linux" },
            { OsFamily.Linux, "Fedora Linux" },
            { OsFamily.Android, "Android 15" },
            { OsFamily.MacOS, "macOS Sonoma" },
            { OsFamily.FreeBSD, "FreeBSD 14.1" },
            { OsFamily.Unknown, "Unknown" }
        };

    [Theory]
    [MemberData(nameof(AllOsFamilies))]
    public void GetLogo_AllFamilies_ReturnValidSpectreMarkup(OsFamily family, string osName)
    {
        var (lines, accentColor) = AsciiArt.GetLogo(family, osName);

        Assert.NotEmpty(lines);
        Assert.False(string.IsNullOrWhiteSpace(accentColor));

        // Ensure Spectre parses all lines without throwing Markup exceptions
        foreach (var line in lines)
        {
            var exception = Record.Exception(() => new Markup(line));
            Assert.Null(exception);
        }

        // Ensure accentColor is valid Spectre style
        var styleException = Record.Exception(() => Style.Parse(accentColor));
        Assert.Null(styleException);
    }

    [Theory]
    [InlineData("arch")]
    [InlineData("ubuntu")]
    [InlineData("debian")]
    [InlineData("gentoo")]
    [InlineData("android")]
    [InlineData("windows")]
    [InlineData("macos")]
    [InlineData("tux")]
    public void GetLogo_CustomLogos_ReturnValidSpectreMarkup(string logoName)
    {
        var (lines, accentColor) = AsciiArt.GetLogo(OsFamily.Windows, "Windows", customLogo: logoName);

        Assert.NotEmpty(lines);
        foreach (var line in lines)
        {
            var exception = Record.Exception(() => new Markup(line));
            Assert.Null(exception);
        }
    }

    [Fact]
    public void GetLogo_InvalidCustomColor_FallsBackGracefullyWithoutCrashing()
    {
        var (lines, accentColor) = AsciiArt.GetLogo(OsFamily.Linux, "Arch Linux", customAccentColor: "not-a-valid-color-12345");

        Assert.NotEmpty(lines);
        Assert.Equal("cyan1", accentColor); // Arch default
    }
}
