using System.Diagnostics;
using SharpFetch.Core.Platforms.Common;
using Xunit;

namespace SharpFetch.Tests;

public class ProcessRunnerTests
{
    [Fact]
    public void TryRun_CommandSucceeds_ReturnsStdout()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Uses /bin/sh");

        string? output = ProcessRunner.TryRun("/bin/sh", "-c \"echo hello\"", 5000);

        Assert.Equal("hello", output?.Trim());
    }

    [Fact]
    public void TryRun_CommandOverrunsTimeout_ReturnsNullWithoutWaiting()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Uses /bin/sh");

        var stopwatch = Stopwatch.StartNew();
        string? output = ProcessRunner.TryRun("/bin/sh", "-c \"sleep 10\"", 200);
        stopwatch.Stop();

        Assert.Null(output);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"Took {stopwatch.Elapsed}");
    }

    [Fact]
    public void TryRun_MissingExecutable_ReturnsNull()
    {
        Assert.Null(ProcessRunner.TryRun("/nonexistent/sharpfetch-test-binary", "", 1000));
    }
}
