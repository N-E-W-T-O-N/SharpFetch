using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms;

[SupportedOSPlatform("macos")]
public sealed partial class MacOsCpuProbe : ICpuProbe
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

    private static int GetSysctlInt32(string name)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        nuint len = (nuint)buffer.Length;
        return sysctlbyname(name, buffer, ref len, IntPtr.Zero, 0) == 0 && len >= sizeof(int)
            ? MemoryMarshal.Read<int>(buffer)
            : 0;
    }

    private static long GetSysctlInt64(string name)
    {
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        nuint len = (nuint)buffer.Length;
        return sysctlbyname(name, buffer, ref len, IntPtr.Zero, 0) == 0 && len >= sizeof(long)
            ? MemoryMarshal.Read<long>(buffer)
            : 0;
    }

    public CpuInfo Detect()
    {
        string model = GetSysctlString("machdep.cpu.brand_string");
        if (model.Length == 0)
        {
            // machdep.cpu.brand_string is an Intel-era sysctl; on Apple Silicon,
            // hw.model ("Mac14,3") is mapped to the friendly Apple M-series chip name.
            string hwModel = GetSysctlString("hw.model");
            model = ResolveAppleSiliconName(hwModel);
        }

        string vendor = GetSysctlString("machdep.cpu.vendor");
        if (vendor.Length == 0 && RuntimeInformation.OSArchitecture == Architecture.Arm64)
        {
            // machdep.cpu.vendor is x86-only; Apple Silicon has no equivalent sysctl.
            vendor = "Apple";
        }

        int physicalCores = GetSysctlInt32("hw.physicalcpu");
        long baseHz = GetSysctlInt64("hw.cpufrequency");

        return new CpuInfo
        {
            Model = model,
            Vendor = vendor,
            PhysicalCores = physicalCores > 0 ? physicalCores : Environment.ProcessorCount,
            LogicalProcessors = Environment.ProcessorCount,
            // hw.cpufrequency does not exist on Apple Silicon - Apple doesn't expose
            // a single clock speed for its P/E core clusters. 0 means "unknown" here,
            // the same sentinel every other platform's probe uses.
            BaseClockMHz = baseHz > 0 ? (int)(baseHz / 1_000_000) : 0
        };
    }

    private static string ResolveAppleSiliconName(string hwModel)
    {
        if (string.IsNullOrEmpty(hwModel)) return "Apple Silicon";

        // M1 family
        if (hwModel is "MacBookAir10,1" or "MacBookPro17,1" or "Macmini9,1" or "iMac21,1" or "iMac21,2") return "Apple M1";
        if (hwModel is "MacBookPro18,1" or "MacBookPro18,2" or "MacBookPro18,3" or "MacBookPro18,4") return "Apple M1 Pro / Max";
        if (hwModel is "Mac13,1" or "Mac13,2") return "Apple M1 Ultra / Max";

        // M2 family
        if (hwModel is "Mac14,2" or "Mac14,7" or "Mac14,15") return "Apple M2";
        if (hwModel is "Mac14,3" or "Mac14,12") return "Apple M2";
        if (hwModel is "Mac14,5" or "Mac14,6" or "Mac14,9" or "Mac14,10") return "Apple M2 Pro / Max";
        if (hwModel is "Mac14,13" or "Mac14,14") return "Apple M2 Ultra";

        // M3 family
        if (hwModel is "Mac15,3" or "Mac15,6" or "Mac15,12" or "Mac15,13") return "Apple M3";
        if (hwModel is "Mac15,7" or "Mac15,8" or "Mac15,9" or "Mac15,10" or "Mac15,11") return "Apple M3 Pro / Max";

        // M4 family
        if (hwModel.StartsWith("Mac16,", StringComparison.OrdinalIgnoreCase)) return "Apple M4";

        return $"Apple Silicon ({hwModel})";
    }
}
