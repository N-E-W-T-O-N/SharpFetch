using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace SharpFetch.Core.Platforms;

/// <summary>
/// Minimal P/Invoke surface over gdi32.dll's D3DKMT ("Direct3D Kernel Mode
/// Thunk") functions - the same undocumented-but-stable API fastfetch uses
/// for accurate GPU enumeration on Windows (see fastfetch/src/detection/gpu/
/// gpu_windows.c and d3dkmthk.h, vendored in this repo). Struct layouts and
/// KMTQUERYADAPTERINFOTYPE values below are ported directly from fastfetch's
/// header and empirically verified against real hardware (adapter
/// enumeration, software-device filtering, name, dedicated VRAM, and
/// integrated/discrete type all confirmed byte-for-byte against fastfetch's
/// own output on the same machine).
///
/// Not ported: GPU core clock speed. fastfetch only gets that via
/// vendor-specific driver libraries (Intel IGCL, NVIDIA NVAPI, AMD ADL) as a
/// fallback once the generic D3DKMT_NODEPERFDATA query returns 0 - which it
/// did here too, on Intel integrated graphics. That's a separate, much
/// larger per-vendor undertaking and out of scope for this probe.
/// </summary>
[SupportedOSPlatform("windows")]
internal static partial class D3dKmt
{
    private const int MaxAdapters = 64;

    public const int KMTQAITYPE_GETSEGMENTSIZE = 3;
    public const int KMTQAITYPE_ADAPTERREGISTRYINFO = 8;
    public const int KMTQAITYPE_ADAPTERTYPE = 15;

    // D3DKMT_ADAPTERTYPE is a bitfield packed into a single UINT; only the
    // bits this probe reads are named here.
    public const uint AdapterTypeSoftwareDevice = 0x04;
    public const uint AdapterTypeHybridDiscrete = 0x10;
    public const uint AdapterTypeHybridIntegrated = 0x20;

    [LibraryImport("gdi32.dll")]
    private static partial int D3DKMTEnumAdapters2(ref EnumAdapters2 enumAdapters);

    [LibraryImport("gdi32.dll")]
    private static partial int D3DKMTQueryAdapterInfo(ref QueryAdapterInfo queryAdapterInfo);

    [LibraryImport("gdi32.dll")]
    private static partial int D3DKMTCloseAdapter(ref CloseAdapter closeAdapter);

    [StructLayout(LayoutKind.Sequential)]
    private struct Luid
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AdapterInfo
    {
        public uint HAdapter;
        public Luid AdapterLuid;
        public uint NumOfSources;
        public int BPrecisePresentRegionsPreferred;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EnumAdapters2
    {
        public uint NumAdapters;
        public IntPtr PAdapters;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct QueryAdapterInfo
    {
        public uint HAdapter;
        public int Type;
        public IntPtr PPrivateDriverData;
        public uint PrivateDriverDataSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CloseAdapter
    {
        public uint HAdapter;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SegmentSizeInfo
    {
        public ulong DedicatedVideoMemorySize;
        public ulong DedicatedSystemMemorySize;
        public ulong SharedSystemMemorySize;
    }

    public readonly record struct Adapter(uint Handle, uint AdapterType, string Name);

    /// <summary>
    /// Enumerates all D3DKMT-visible adapters, excluding software/virtual
    /// devices (e.g. the "Microsoft Remote Display Adapter" a Windows host
    /// exposes over RDP) the same way fastfetch does - by checking the
    /// AdapterType.SoftwareDevice bit rather than matching on device names.
    /// </summary>
    public static List<Adapter> EnumerateHardwareAdapters()
    {
        var result = new List<Adapter>();

        int adapterInfoSize = Marshal.SizeOf<AdapterInfo>();
        IntPtr adaptersBuffer = Marshal.AllocHGlobal(adapterInfoSize * MaxAdapters);
        try
        {
            var enumAdapters = new EnumAdapters2 { NumAdapters = MaxAdapters, PAdapters = adaptersBuffer };
            if (D3DKMTEnumAdapters2(ref enumAdapters) < 0)
            {
                return result;
            }

            for (int i = 0; i < enumAdapters.NumAdapters; i++)
            {
                var info = Marshal.PtrToStructure<AdapterInfo>(adaptersBuffer + i * adapterInfoSize);

                // Every enumerated handle must end up either closed here or added to
                // `result` (the caller closes those after use) - if querying this one
                // adapter throws partway through, closing-and-skipping just that
                // adapter is safer than letting the exception escape: propagating it
                // would drop `result` entirely, leaking every handle already added
                // for adapters processed earlier in this same loop.
                try
                {
                    uint adapterType = 0;
                    bool typeOk = TryQuery(info.HAdapter, KMTQAITYPE_ADAPTERTYPE, ref adapterType);

                    if (typeOk && (adapterType & AdapterTypeSoftwareDevice) != 0)
                    {
                        CloseHandle(info.HAdapter);
                        continue;
                    }

                    result.Add(new Adapter(info.HAdapter, adapterType, GetAdapterName(info.HAdapter)));
                }
                catch
                {
                    CloseHandle(info.HAdapter);
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(adaptersBuffer);
        }

        return result;
    }

    public static bool TryGetSegmentSize(uint hAdapter, out SegmentSizeInfo info)
    {
        info = default;
        return TryQuery(hAdapter, KMTQAITYPE_GETSEGMENTSIZE, ref info);
    }

    public static void CloseHandle(uint hAdapter)
    {
        var close = new CloseAdapter { HAdapter = hAdapter };
        D3DKMTCloseAdapter(ref close);
    }

    private static string GetAdapterName(uint hAdapter)
    {
        // D3DKMT_ADAPTERREGISTRYINFO is 4x WCHAR[260]; only the first field
        // (AdapterString) is needed, but the query requires the full buffer size.
        const int fieldChars = 260;
        const int fieldCount = 4;
        int bufferSize = fieldChars * fieldCount * sizeof(char);

        IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            var query = new QueryAdapterInfo
            {
                HAdapter = hAdapter,
                Type = KMTQAITYPE_ADAPTERREGISTRYINFO,
                PPrivateDriverData = buffer,
                PrivateDriverDataSize = (uint)bufferSize
            };

            return D3DKMTQueryAdapterInfo(ref query) >= 0
                ? Marshal.PtrToStringUni(buffer) ?? string.Empty
                : string.Empty;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static bool TryQuery<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>(
        uint hAdapter, int type, ref T data) where T : unmanaged
    {
        int size = Marshal.SizeOf<T>();
        IntPtr buffer = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(data, buffer, false);
            var query = new QueryAdapterInfo
            {
                HAdapter = hAdapter,
                Type = type,
                PPrivateDriverData = buffer,
                PrivateDriverDataSize = (uint)size
            };

            if (D3DKMTQueryAdapterInfo(ref query) < 0)
            {
                return false;
            }

            data = Marshal.PtrToStructure<T>(buffer);
            return true;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}
