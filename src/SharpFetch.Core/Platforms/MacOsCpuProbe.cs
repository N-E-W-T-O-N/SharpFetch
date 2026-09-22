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
            // machdep.cpu.brand_string is an Intel-era sysctl; whether Apple
            // Silicon populates it varies by macOS version. hw.model
            // ("Mac14,3") is always present but is a machine identifier, not
            // a chip name - better than nothing, worse than a real brand string.
            model = GetSysctlString("hw.model");
        }
        if (model.Length == 0)
        {
            model = "Unknown CPU";
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
}
