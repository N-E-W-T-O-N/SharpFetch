using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms;

[SupportedOSPlatform("freebsd")]
public sealed partial class FreeBSDProbe : IOsProbe
{
    [LibraryImport("libc", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int sysctlbyname(
        string name,
        Span<byte> oldp,
        ref nuint oldlenp,
        IntPtr newp,
        nuint newlen);

    private static string GetSysctlString(string name)
    {
        Span<byte> buffer = stackalloc byte[256];
        nuint len = (nuint)buffer.Length;
        if (sysctlbyname(name, buffer, ref len, IntPtr.Zero, 0) == 0 && len > 1)
        {
            // Omit null terminator
            return Encoding.UTF8.GetString(buffer[..(int)(len - 1)]);
        }
        return string.Empty;
    }

    public OsInfo Detect()
    {
        // kern.ostype is "FreeBSD"; kern.osrelease looks like "15.1-RELEASE-p2".
        string osType = GetSysctlString("kern.ostype");
        string osRelease = GetSysctlString("kern.osrelease");

        string name = !string.IsNullOrEmpty(osType) ? osType : "FreeBSD";

        // Split "15.1-RELEASE-p2" into version "15.1" and build "RELEASE-p2".
        string version = osRelease;
        string build = string.Empty;
        int dashIndex = osRelease.IndexOf('-');
        if (dashIndex > 0)
        {
            version = osRelease[..dashIndex];
            build = osRelease[(dashIndex + 1)..];
        }

        string kernel = !string.IsNullOrEmpty(osRelease)
            ? $"{name} {osRelease}"
            : Environment.OSVersion.Version.ToString();

        // hw.machine is authoritative on FreeBSD ("amd64", "arm64", "i386") and is
        // what a FreeBSD user expects to see rather than a Linux-style normalization.
        string arch = GetSysctlString("hw.machine");
        if (string.IsNullOrEmpty(arch))
        {
            arch = RuntimeInformation.OSArchitecture switch
            {
                Architecture.X64 => "amd64",
                Architecture.Arm64 => "arm64",
                Architecture.X86 => "i386",
                Architecture.Arm => "armv7",
                _ => RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant()
            };
        }

        return new OsInfo
        {
            Name = name,
            Version = version,
            Build = build,
            Kernel = kernel,
            Architecture = arch,
            Family = OsFamily.FreeBSD,
            Codename = null,
            Hostname = Environment.MachineName,
            Username = Environment.UserName,
            Uptime = GetUptime()
        };
    }

    private static TimeSpan GetUptime()
    {
        // kern.boottime returns struct timeval { time_t tv_sec; suseconds_t tv_usec; }.
        // Only tv_sec — the leading 64-bit field on 64-bit FreeBSD — is needed here.
        Span<byte> buffer = stackalloc byte[16];
        nuint len = (nuint)buffer.Length;
        if (sysctlbyname("kern.boottime", buffer, ref len, IntPtr.Zero, 0) == 0 && len >= sizeof(long))
        {
            long bootSeconds = MemoryMarshal.Read<long>(buffer);
            if (bootSeconds > 0)
            {
                TimeSpan uptime = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(bootSeconds);
                if (uptime > TimeSpan.Zero)
                {
                    return uptime;
                }
            }
        }

        return TimeSpan.FromMilliseconds(Environment.TickCount64);
    }
}
