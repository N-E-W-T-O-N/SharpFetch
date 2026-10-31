using System.Runtime.Versioning;
using SharpFetch.Core.Platforms.Common;

namespace SharpFetch.Core.Platforms.MacOS;

/// <summary>
/// The GPU and display probes both read `system_profiler SPDisplaysDataType
/// -json` (~50-150 ms). Caching it for the process lifetime runs it once per
/// fetch instead of once per probe.
/// </summary>
[SupportedOSPlatform("macos")]
internal static class SystemProfiler
{
    private const int DisplaysTimeoutMs = 10000;

    private static readonly Lazy<string?> DisplaysJsonCache = new(() =>
        ProcessRunner.TryRun("/usr/sbin/system_profiler", "SPDisplaysDataType -json", DisplaysTimeoutMs));

    public static string? DisplaysJson => DisplaysJsonCache.Value;
}
