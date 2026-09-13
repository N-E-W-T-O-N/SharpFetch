using SharpFetch.Core.Models;
using SharpFetch.Core.Modules;
using SharpFetch.Core.Probes;
using Xunit;

namespace SharpFetch.Tests;

public class FetchEngineTests
{
    private sealed class DummyOsProbe : IOsProbe
    {
        public OsInfo Detect() => new()
        {
            Name = "Test OS",
            Version = "1.0",
            Build = "100",
            Kernel = "1.0.0",
            Architecture = "x86_64",
            Family = OsFamily.Linux,
            Hostname = "testhost",
            Username = "testuser",
            Uptime = TimeSpan.FromHours(2)
        };
    }

    [Fact]
    public void CreateDefault_DoesNotContainDuplicateModules()
    {
        var engine = FetchEngine.CreateDefault(new DummyOsProbe());

        var moduleKeys = engine.Modules.Select(m => m.Metadata.Key).ToList();
        var uniqueKeys = moduleKeys.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        Assert.Equal(uniqueKeys.Count, moduleKeys.Count);
    }

    [Fact]
    public void Execute_WithEnabledModules_FiltersBeforeExecution()
    {
        var engine = FetchEngine.CreateDefault(new DummyOsProbe());

        var results = engine.Execute(new[] { "OS", "Uptime" }, null);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Key.Equals("OS", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(results, r => r.Key.Equals("Uptime", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Execute_WithEnabledModules_PreservesUserRequestedOrder()
    {
        var engine = FetchEngine.CreateDefault(new DummyOsProbe());

        var results = engine.Execute(new[] { "Uptime", "OS" }, null);

        Assert.Equal(2, results.Count);
        Assert.Equal("Uptime", results[0].Key);
        Assert.Equal("OS", results[1].Key);
    }

    [Fact]
    public void Execute_WithDisabledModules_ExcludesThem()
    {
        var engine = FetchEngine.CreateDefault(new DummyOsProbe());

        var allResults = engine.ExecuteAll();
        var filteredResults = engine.Execute(null, new[] { "CPU", "GPU" });

        Assert.DoesNotContain(filteredResults, r => r.Key.Equals("CPU", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(filteredResults, r => r.Key.Equals("GPU", StringComparison.OrdinalIgnoreCase));
        Assert.True(filteredResults.Count < allResults.Count);
    }
}
