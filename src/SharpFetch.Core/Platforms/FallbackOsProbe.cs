using System.Runtime.InteropServices;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms;

public sealed class FallbackOsProbe : IOsProbe
{
    public OsInfo Detect()
    {
        string desc = RuntimeInformation.OSDescription;
        string arch = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x86_64",
            Architecture.Arm64 => "arm64",
            Architecture.X86 => "i686",
            Architecture.Arm => "armv7l",
            _ => RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant()
        };

        OsFamily family = OsFamily.Unknown;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) family = OsFamily.Windows;
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) family = OsFamily.Linux;
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) family = OsFamily.MacOS;
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)) family = OsFamily.FreeBSD;

        return new OsInfo
        {
            Name = desc,
            Version = Environment.OSVersion.VersionString,
            Build = Environment.OSVersion.Version.Build.ToString(),
            Kernel = Environment.OSVersion.Version.ToString(),
            Architecture = arch,
            Family = family,
            Hostname = Environment.MachineName,
            Username = Environment.UserName,
            Uptime = TimeSpan.FromMilliseconds(Environment.TickCount64)
        };
    }
}
