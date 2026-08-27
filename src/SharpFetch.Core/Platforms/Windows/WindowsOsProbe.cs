using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Platforms.Windows;

[SupportedOSPlatform("windows")]
public sealed class WindowsOsProbe : IOsProbe
{
    public OsInfo Detect()
    {
        string productName = "Windows";
        string displayVersion = "";
        string buildNumber = "";
        int ubr = 0;
        string edition = "";

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key != null)
            {
                productName = key.GetValue("ProductName") as string ?? "Windows";
                displayVersion = key.GetValue("DisplayVersion") as string 
                    ?? key.GetValue("ReleaseId") as string 
                    ?? "";
                buildNumber = key.GetValue("CurrentBuildNumber") as string 
                    ?? key.GetValue("CurrentBuild") as string 
                    ?? "";
                
                if (key.GetValue("UBR") is int ubrVal)
                {
                    ubr = ubrVal;
                }

                edition = key.GetValue("EditionID") as string ?? "";
            }
        }
        catch
        {
            // Ignore registry read errors and fallback to BCL
        }

        // Windows 11 fix: Windows 11 builds (>= 22000) may report "Windows 10" in ProductName
        if (int.TryParse(buildNumber, out int buildInt) && buildInt >= 22000)
        {
            if (productName.StartsWith("Windows 10", StringComparison.OrdinalIgnoreCase))
            {
                productName = "Windows 11" + productName["Windows 10".Length..];
            }
        }

        string fullBuild = ubr > 0 && !string.IsNullOrEmpty(buildNumber) 
            ? $"{buildNumber}.{ubr}" 
            : buildNumber;

        string arch = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x86_64",
            Architecture.Arm64 => "arm64",
            Architecture.X86 => "i686",
            Architecture.Arm => "armv7l",
            _ => RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant()
        };

        string kernel = Environment.OSVersion.Version.ToString();

        return new OsInfo
        {
            Name = productName,
            Version = displayVersion,
            Build = fullBuild,
            Kernel = kernel,
            Architecture = arch,
            Family = OsFamily.Windows,
            Codename = !string.IsNullOrEmpty(displayVersion) ? displayVersion : null,
            Hostname = Environment.MachineName,
            Username = Environment.UserName,
            Uptime = TimeSpan.FromMilliseconds(Environment.TickCount64)
        };
    }
}
