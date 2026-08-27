using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Platforms.MacOS;

[SupportedOSPlatform("macos")]
public sealed partial class MacOsProbe : IOsProbe
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
            return System.Text.Encoding.UTF8.GetString(buffer[..(int)(len - 1)]);
        }
        return string.Empty;
    }

    public OsInfo Detect()
    {
        string name = "macOS";
        string version = GetSysctlString("kern.osproductversion");
        string build = GetSysctlString("kern.osversion");
        string osRelease = GetSysctlString("kern.osrelease");
        string osType = GetSysctlString("kern.ostype");

        string kernel = !string.IsNullOrEmpty(osRelease) 
            ? $"Darwin {osRelease}" 
            : Environment.OSVersion.Version.ToString();

        string codename = GetMacOsCodeName(version);

        string arch = RuntimeInformation.OSArchitecture switch
        {
            Architecture.Arm64 => "arm64",
            Architecture.X64 => "x86_64",
            _ => RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant()
        };

        return new OsInfo
        {
            Name = name,
            Version = version,
            Build = build,
            Kernel = kernel,
            Architecture = arch,
            Family = OsFamily.MacOS,
            Codename = codename,
            Hostname = Environment.MachineName,
            Username = Environment.UserName,
            Uptime = TimeSpan.FromMilliseconds(Environment.TickCount64)
        };
    }

    private static string GetMacOsCodeName(string version)
    {
        if (string.IsNullOrWhiteSpace(version)) return string.Empty;

        string majorStr = version.Split('.')[0];
        if (int.TryParse(majorStr, out int major))
        {
            return major switch
            {
                26 or 16 => "Tahoe",
                15 => "Sequoia",
                14 => "Sonoma",
                13 => "Ventura",
                12 => "Monterey",
                11 => "Big Sur",
                10 => GetMacOs10CodeName(version),
                _ => string.Empty
            };
        }

        return string.Empty;
    }

    private static string GetMacOs10CodeName(string version)
    {
        string[] parts = version.Split('.');
        if (parts.Length > 1 && int.TryParse(parts[1], out int minor))
        {
            return minor switch
            {
                16 => "Big Sur",
                15 => "Catalina",
                14 => "Mojave",
                13 => "High Sierra",
                12 => "Sierra",
                11 => "El Capitan",
                10 => "Yosemite",
                9 => "Mavericks",
                _ => string.Empty
            };
        }
        return string.Empty;
    }
}
