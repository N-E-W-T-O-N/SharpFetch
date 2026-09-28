using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms;

[SupportedOSPlatform("freebsd")]
public sealed partial class FreeBSDCpuProbe : ICpuProbe
{
    [LibraryImport("libc", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int sysctlbyname(
        string name,
        Span<byte> oldp,
        ref nuint oldlenp,
        IntPtr newp,
        nuint newlen);

    public CpuInfo Detect()
    {
        string model = GetSysctlString("hw.model");
        if (string.IsNullOrEmpty(model))
        {
            model = "Unknown CPU";
        }

        int ncpu = GetSysctlInt32("hw.ncpu");
        if (ncpu <= 0)
        {
            ncpu = Environment.ProcessorCount;
        }

        string vendor = string.Empty;
        if (model.Contains("AMD", StringComparison.OrdinalIgnoreCase))
        {
            vendor = "AMD";
        }
        else if (model.Contains("Intel", StringComparison.OrdinalIgnoreCase))
        {
            vendor = "Intel";
        }

        return new CpuInfo
        {
            Model = model,
            Vendor = vendor,
            PhysicalCores = ncpu,
            LogicalProcessors = ncpu,
            BaseClockMHz = 0
        };
    }

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

    private static int GetSysctlInt32(string name)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        nuint len = (nuint)buffer.Length;
        return sysctlbyname(name, buffer, ref len, IntPtr.Zero, 0) == 0 && len >= sizeof(int)
            ? MemoryMarshal.Read<int>(buffer)
            : 0;
    }
}
