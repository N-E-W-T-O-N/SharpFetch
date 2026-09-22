using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms;

[SupportedOSPlatform("linux")]
public sealed class LinuxOsProbe : IOsProbe
{
    public OsInfo Detect()
    {
        string name = "Linux";
        string version = "";
        string build = "";
        string codename = "";
        string prettyName = "";

        // 1. Parse /etc/os-release or /usr/lib/os-release
        string osReleasePath = File.Exists("/etc/os-release") ? "/etc/os-release" : "/usr/lib/os-release";
        if (File.Exists(osReleasePath))
        {
            try
            {
                foreach (string line in File.ReadLines(osReleasePath))
                {
                    int eqIndex = line.IndexOf('=');
                    if (eqIndex <= 0) continue;

                    string key = line[..eqIndex].Trim();
                    string value = line[(eqIndex + 1)..].Trim().Trim('"', '\'');

                    switch (key)
                    {
                        case "PRETTY_NAME":
                            prettyName = value;
                            break;
                        case "NAME":
                            name = value;
                            break;
                        case "VERSION_ID":
                            version = value;
                            break;
                        case "VERSION":
                            if (string.IsNullOrEmpty(version)) version = value;
                            break;
                        case "VERSION_CODENAME":
                        case "CODENAME":
                            codename = value;
                            break;
                        case "BUILD_ID":
                            build = value;
                            break;
                    }
                }
            }
            catch
            {
                // Fallback to defaults
            }
        }

        // If PRETTY_NAME is present and name is basic, PRETTY_NAME can refine it
        if (!string.IsNullOrEmpty(prettyName) && name == "Linux")
        {
            name = prettyName;
        }

        // 2. Kernel release from /proc/sys/kernel/osrelease
        string kernel = "";
        try
        {
            if (File.Exists("/proc/sys/kernel/osrelease"))
            {
                kernel = File.ReadAllText("/proc/sys/kernel/osrelease").Trim();
            }
        }
        catch
        {
            // Ignore
        }

        if (string.IsNullOrEmpty(kernel))
        {
            kernel = Environment.OSVersion.Version.ToString();
        }

        // 3. Normalized Architecture
        string arch = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x86_64",
            Architecture.Arm64 => "aarch64",
            Architecture.X86 => "i686",
            Architecture.Arm => "armv7l",
            _ => RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant()
        };

        // 4. Uptime from /proc/uptime
        TimeSpan uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
        try
        {
            if (File.Exists("/proc/uptime"))
            {
                string text = File.ReadAllText("/proc/uptime");
                string firstNum = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
                if (double.TryParse(firstNum, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds))
                {
                    uptime = TimeSpan.FromSeconds(seconds);
                }
            }
        }
        catch
        {
            // Ignore
        }

        return new OsInfo
        {
            Name = name,
            Version = version,
            Build = build,
            Kernel = kernel,
            Architecture = arch,
            Family = OsFamily.Linux,
            Codename = !string.IsNullOrEmpty(codename) ? codename : null,
            Hostname = Environment.MachineName,
            Username = Environment.UserName,
            Uptime = uptime
        };
    }
}
