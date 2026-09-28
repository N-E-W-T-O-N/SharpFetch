using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms.MacOS;

public sealed partial class MacOsHostProbe : IHostProbe
{
    [LibraryImport("libc", StringMarshalling = StringMarshalling.Utf8)]
    [SupportedOSPlatform("macos")]
    private static partial int sysctlbyname(
        string name,
        Span<byte> oldp,
        ref nuint oldlenp,
        IntPtr newp,
        nuint newlen);

    [SupportedOSPlatform("macos")]
    public HostInfo? Detect()
    {
        string model = GetSysctlString("hw.model");
        if (string.IsNullOrEmpty(model))
        {
            return null;
        }

        string friendly = ResolveMacModel(model);
        string name = !string.IsNullOrEmpty(friendly) ? $"{friendly} ({model})" : model;

        return new HostInfo
        {
            Name = name,
            Vendor = "Apple",
            Family = "Mac"
        };
    }

    public static string ResolveMacModel(string model)
    {
        return model switch
        {
            // Apple Silicon
            "MacBookAir10,1" => "MacBook Air (M1, 2020)",
            "MacBookPro17,1" => "MacBook Pro 13\" (M1, 2020)",
            "Macmini9,1" => "Mac mini (M1, 2020)",
            "iMac21,1" or "iMac21,2" => "iMac 24\" (M1, 2021)",
            "MacBookPro18,1" or "MacBookPro18,2" => "MacBook Pro 16\" (M1 Pro/Max, 2021)",
            "MacBookPro18,3" or "MacBookPro18,4" => "MacBook Pro 14\" (M1 Pro/Max, 2021)",
            "Mac13,1" or "Mac13,2" => "Mac Studio (M1 Max/Ultra, 2022)",
            "Mac14,2" => "MacBook Air 13\" (M2, 2022)",
            "Mac14,15" => "MacBook Air 15\" (M2, 2023)",
            "Mac14,7" => "MacBook Pro 13\" (M2, 2022)",
            "Mac14,3" or "Mac14,12" => "Mac mini (M2, 2023)",
            "Mac14,5" or "Mac14,6" => "MacBook Pro 14\" (M2 Pro/Max, 2023)",
            "Mac14,9" or "Mac14,10" => "MacBook Pro 16\" (M2 Pro/Max, 2023)",
            "Mac14,13" or "Mac14,14" => "Mac Studio (M2 Max/Ultra, 2023)",
            "Mac14,8" => "Mac Pro (M2 Ultra, 2023)",
            "Mac15,3" => "MacBook Pro 14\" (M3, 2023)",
            "Mac15,4" => "iMac 24\" (M3, 2023)",
            "Mac15,6" or "Mac15,8" => "MacBook Pro 14\" (M3 Pro/Max, 2023)",
            "Mac15,7" or "Mac15,9" or "Mac15,11" => "MacBook Pro 16\" (M3 Pro/Max, 2023)",
            "Mac15,12" => "MacBook Air 13\" (M3, 2024)",
            "Mac15,13" => "MacBook Air 15\" (M3, 2024)",
            _ => model.StartsWith("Mac16,", StringComparison.OrdinalIgnoreCase) ? "Mac (M4, 2024)" : string.Empty
        };
    }

    [SupportedOSPlatform("macos")]
    private static string GetSysctlString(string name)
    {
        Span<byte> buffer = stackalloc byte[256];
        nuint len = (nuint)buffer.Length;
        if (sysctlbyname(name, buffer, ref len, IntPtr.Zero, 0) == 0 && len > 1)
        {
            return Encoding.UTF8.GetString(buffer[..(int)(len - 1)]);
        }
        return string.Empty;
    }
}
