using SharpFetch.Cli;
using Xunit;

namespace SharpFetch.Tests;

public class CliParserTests
{
    [Theory]
    [InlineData("-v", CliAction.ShowVersion)]
    [InlineData("--version", CliAction.ShowVersion)]
    [InlineData("-h", CliAction.ShowHelp)]
    [InlineData("--help", CliAction.ShowHelp)]
    [InlineData("-?", CliAction.ShowHelp)]
    [InlineData("--list-modules", CliAction.ListModules)]
    public void Parse_EarlyExitActions_ReturnsExpectedAction(string flag, CliAction expectedAction)
    {
        var options = CliParser.Parse([flag]);
        Assert.Equal(expectedAction, options.Action);
    }

    [Fact]
    public void Parse_BooleanFlags_SetsCorrectProperties()
    {
        var options = CliParser.Parse(["--no-logo", "--no-color", "--no-palette"]);

        Assert.True(options.NoLogo);
        Assert.True(options.DisableColor);
        Assert.False(options.ShowColorPalette);
        Assert.Equal(CliAction.RunFetch, options.Action);
    }

    [Fact]
    public void Parse_SpaceDelimitedOptions_ParsesValues()
    {
        var options = CliParser.Parse(["--logo", "arch", "--color", "cyan", "--config", "/etc/sharpfetch.conf"]);

        Assert.Equal("arch", options.CustomLogo);
        Assert.Equal("cyan", options.AccentColor);
        Assert.Equal("/etc/sharpfetch.conf", options.ConfigPath);
        Assert.Equal(CliAction.RunFetch, options.Action);
    }

    [Fact]
    public void Parse_InlineEqualsOptions_ParsesValues()
    {
        var options = CliParser.Parse(["--logo=ubuntu", "--color=magenta"]);

        Assert.Equal("ubuntu", options.CustomLogo);
        Assert.Equal("magenta", options.AccentColor);
    }

    [Fact]
    public void Parse_UnixAbsolutePath_DoesNotTreatPathAsSwitch()
    {
        var options = CliParser.Parse(["-c", "/var/log/sharpfetch.json"]);

        Assert.Equal("/var/log/sharpfetch.json", options.ConfigPath);
        Assert.Equal(CliAction.RunFetch, options.Action);
    }

    [Fact]
    public void Parse_CommaSeparatedModules_PopulatesEnabledModules()
    {
        var options = CliParser.Parse(["-m", "os,cpu,memory,uptime"]);

        Assert.Equal(["os", "cpu", "memory", "uptime"], options.EnabledModules);
    }

    [Fact]
    public void Parse_RepeatedModuleOptions_PopulatesEnabledModules()
    {
        var options = CliParser.Parse(["--module", "os", "--module", "gpu"]);

        Assert.Equal(["os", "gpu"], options.EnabledModules);
    }

    [Fact]
    public void Parse_MissingOptionValue_ReturnsShowError()
    {
        var options = CliParser.Parse(["--logo"]);

        Assert.Equal(CliAction.ShowError, options.Action);
        Assert.NotNull(options.ErrorMessage);
        Assert.Contains("requires a value", options.ErrorMessage);
    }

    [Fact]
    public void Parse_InlineEmptyOptionValue_ReturnsShowError()
    {
        var options = CliParser.Parse(["--logo="]);

        Assert.Equal(CliAction.ShowError, options.Action);
        Assert.NotNull(options.ErrorMessage);
        Assert.Contains("requires a value", options.ErrorMessage);
    }

    [Fact]
    public void Parse_UnrecognizedOption_ReturnsShowError()
    {
        var options = CliParser.Parse(["--unknown-flag"]);

        Assert.Equal(CliAction.ShowError, options.Action);
        Assert.NotNull(options.ErrorMessage);
        Assert.Contains("Unrecognized option", options.ErrorMessage);
    }
}
