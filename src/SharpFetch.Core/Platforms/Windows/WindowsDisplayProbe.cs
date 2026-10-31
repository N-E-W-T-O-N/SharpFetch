using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
using SharpFetch.Core.Models;
using SharpFetch.Core.Platforms.Common;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms.Windows;

/// <summary>
/// Enumerates displays through QueryDisplayConfig, one entry per active
/// display path (source -> target), the same approach fastfetch takes.
/// EnumDisplayMonitors reports desktop regions instead, so a laptop panel
/// duplicated onto an external monitor collapses into a single "monitor";
/// display paths keep each physical screen separate. Each path's target
/// carries its own friendly name, output technology and device path, so the
/// EDID is read from that exact device's registry key rather than matched by
/// index against every monitor ever connected.
/// </summary>
public sealed class WindowsDisplayProbe : IDisplayProbe
{
    private const uint QdcOnlyActivePaths = 0x2;
    private const uint DisplayConfigPathActive = 0x1;
    private const uint ModeInfoTypeSource = 1;
    private const uint ModeInfoTypeTarget = 2;
    private const uint DeviceInfoGetTargetName = 2;

    [SupportedOSPlatform("windows")]
    public IReadOnlyList<DisplayInfo> DetectDisplays()
    {
        if (!OperatingSystem.IsWindows())
            return [];

        try
        {
            List<DisplayInfo> results = DetectViaDisplayConfig();
            return results.Count > 0 ? results : DetectViaMonitors();
        }
        catch
        {
            return [];
        }
    }

    [SupportedOSPlatform("windows")]
    private static List<DisplayInfo> DetectViaDisplayConfig()
    {
        var results = new List<DisplayInfo>();

        DISPLAYCONFIG_PATH_INFO[] paths;
        DISPLAYCONFIG_MODE_INFO[] modes;
        uint pathCount, modeCount;

        // The topology can change between sizing and querying (a monitor
        // plugged in); ERROR_INSUFFICIENT_BUFFER means "size again".
        const int ErrorInsufficientBuffer = 122;
        int status;
        do
        {
            if (GetDisplayConfigBufferSizes(QdcOnlyActivePaths, out pathCount, out modeCount) != 0)
                return results;

            paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
            modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
            status = QueryDisplayConfig(QdcOnlyActivePaths, ref pathCount, paths, ref modeCount, modes, nint.Zero);
        }
        while (status == ErrorInsufficientBuffer);

        if (status != 0)
            return results;

        for (int i = 0; i < pathCount; i++)
        {
            DISPLAYCONFIG_PATH_INFO path = paths[i];
            if ((path.flags & DisplayConfigPathActive) == 0)
                continue;

            int width = 0, height = 0;
            bool isPrimary = false;
            uint sourceIdx = path.sourceInfo.modeInfoIdx;
            if (sourceIdx < modeCount && modes[sourceIdx].infoType == ModeInfoTypeSource)
            {
                DISPLAYCONFIG_SOURCE_MODE source = modes[sourceIdx].sourceMode;
                width = (int)source.width;
                height = (int)source.height;
                // The primary display is the one whose desktop origin is (0, 0);
                // in duplicate mode several paths share it, so mark only the first.
                isPrimary = source.positionX == 0 && source.positionY == 0 && !results.Exists(r => r.IsPrimary);
            }

            uint targetIdx = path.targetInfo.modeInfoIdx;
            if ((width == 0 || height == 0) && targetIdx < modeCount && modes[targetIdx].infoType == ModeInfoTypeTarget)
            {
                width = (int)modes[targetIdx].targetMode.activeWidth;
                height = (int)modes[targetIdx].targetMode.activeHeight;
            }

            if (width <= 0 || height <= 0)
                continue;

            double refreshRate = ToHz(path.targetInfo.refreshRate.Numerator, path.targetInfo.refreshRate.Denominator);
            if (refreshRate <= 0 && targetIdx < modeCount && modes[targetIdx].infoType == ModeInfoTypeTarget)
            {
                DISPLAYCONFIG_VIDEO_SIGNAL_INFO signal = modes[targetIdx].targetMode;
                refreshRate = ToHz(signal.vSyncNumerator, signal.vSyncDenominator);
            }

            string? friendlyName = null;
            string? devicePath = null;
            var targetName = new DISPLAYCONFIG_TARGET_DEVICE_NAME();
            targetName.header.type = DeviceInfoGetTargetName;
            targetName.header.size = (uint)Marshal.SizeOf<DISPLAYCONFIG_TARGET_DEVICE_NAME>();
            targetName.header.adapterId = path.targetInfo.adapterId;
            targetName.header.id = path.targetInfo.id;
            if (DisplayConfigGetDeviceInfo(ref targetName) == 0)
            {
                if (!string.IsNullOrWhiteSpace(targetName.monitorFriendlyDeviceName))
                    friendlyName = targetName.monitorFriendlyDeviceName.Trim();
                devicePath = targetName.monitorDevicePath;
            }

            string? edidName = null;
            int? inches = null;
            if (TryReadEdid(devicePath, out byte[]? edid) &&
                EdidParser.TryParse(edid, out string? parsedName, out int? parsedInches))
            {
                edidName = parsedName;
                inches = parsedInches;
            }

            // Output technologies: 0x80000000 (Internal), 11 (DisplayPort Embedded), 13 (UDI Embedded) -> Built-in
            uint tech = path.targetInfo.outputTechnology;
            bool isBuiltin = tech is 0x80000000 or 11 or 13;

            results.Add(new DisplayInfo
            {
                // Laptop panels often have no friendly name; fall back to EDID.
                Name = friendlyName ?? (string.IsNullOrEmpty(edidName) ? "Display" : edidName),
                Width = width,
                Height = height,
                RefreshRate = refreshRate > 0 ? refreshRate : 60.0,
                DiagonalInches = inches,
                Type = isBuiltin ? DisplayType.Builtin : DisplayType.External,
                IsPrimary = isPrimary
            });
        }

        return results;
    }

    /// <summary>
    /// Fallback for when QueryDisplayConfig is unavailable: one entry per
    /// desktop monitor region, without names or EDID data.
    /// </summary>
    private static List<DisplayInfo> DetectViaMonitors()
    {
        var results = new List<DisplayInfo>();

        EnumDisplayMonitors(nint.Zero, nint.Zero, (nint hMonitor, nint hdcMonitor, ref RECT lprcMonitor, nint dwData) =>
        {
            var mi = new MONITORINFOEX();
            mi.cbSize = Marshal.SizeOf<MONITORINFOEX>();

            if (GetMonitorInfo(hMonitor, ref mi))
            {
                int width = mi.rcMonitor.right - mi.rcMonitor.left;
                int height = mi.rcMonitor.bottom - mi.rcMonitor.top;

                var dm = new DEVMODE();
                dm.dmSize = (short)Marshal.SizeOf<DEVMODE>();
                double refreshRate = 60.0;
                if (EnumDisplaySettings(mi.szDevice, -1, ref dm) && dm.dmDisplayFrequency > 0)
                {
                    refreshRate = dm.dmDisplayFrequency;
                    if (dm.dmPelsWidth > 0 && dm.dmPelsHeight > 0)
                    {
                        width = dm.dmPelsWidth;
                        height = dm.dmPelsHeight;
                    }
                }

                results.Add(new DisplayInfo
                {
                    Name = "Display",
                    Width = width,
                    Height = height,
                    RefreshRate = refreshRate,
                    Type = DisplayType.Unknown,
                    IsPrimary = (mi.dwFlags & 1) != 0
                });
            }
            return true;
        }, nint.Zero);

        return results;
    }

    internal static double ToHz(uint numerator, uint denominator) =>
        denominator == 0 ? 0 : Math.Round((double)numerator / denominator, 2);

    /// <summary>
    /// Maps a monitor device interface path such as
    /// <c>\\?\DISPLAY#DELA0A5#5&amp;2e0a1c4&amp;0&amp;UID4352#{e6f07b5f-...}</c>
    /// to its registry key, <c>SYSTEM\CurrentControlSet\Enum\DISPLAY\DELA0A5\5&amp;2e0a1c4&amp;0&amp;UID4352\Device Parameters</c>.
    /// </summary>
    internal static string? DevicePathToRegistryKey(string? devicePath)
    {
        if (string.IsNullOrEmpty(devicePath))
            return null;

        string[] parts = devicePath.Split('#');
        if (parts.Length < 3 ||
            !parts[0].EndsWith("DISPLAY", StringComparison.OrdinalIgnoreCase) ||
            parts[1].Length == 0 || parts[2].Length == 0 ||
            parts[1].Contains('\\') || parts[2].Contains('\\'))
            return null;

        return $@"SYSTEM\CurrentControlSet\Enum\DISPLAY\{parts[1]}\{parts[2]}\Device Parameters";
    }

    [SupportedOSPlatform("windows")]
    private static bool TryReadEdid(string? devicePath, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out byte[]? edid)
    {
        edid = null;
        string? keyPath = DevicePathToRegistryKey(devicePath);
        if (keyPath is null)
            return false;

        try
        {
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(keyPath);
            if (key?.GetValue("EDID") is byte[] bytes && bytes.Length >= 128)
            {
                edid = bytes;
                return true;
            }
        }
        catch
        {
            // Ignore registry permission errors
        }

        return false;
    }

    // P/Invoke definitions
    private delegate bool MonitorEnumProc(nint hMonitor, nint hdcMonitor, ref RECT lprcMonitor, nint dwData);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int left; public int top; public int right; public int bottom; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public int dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID { public uint LowPart; public int HighPart; }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_RATIONAL { public uint Numerator; public uint Denominator; }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_PATH_SOURCE_INFO { public LUID adapterId; public uint id; public uint modeInfoIdx; public uint statusFlags; }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_PATH_TARGET_INFO
    {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        public uint outputTechnology;
        public uint rotation;
        public uint scaling;
        public DISPLAYCONFIG_RATIONAL refreshRate;
        public uint scanLineOrdering;
        public int targetAvailable; // Win32 BOOL
        public uint statusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_PATH_INFO
    {
        public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
        public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
        public uint flags;
    }

    // DISPLAYCONFIG_VIDEO_SIGNAL_INFO (48 bytes): the target-mode arm of the union.
    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_VIDEO_SIGNAL_INFO
    {
        public ulong pixelRate;
        public uint hSyncNumerator;
        public uint hSyncDenominator;
        public uint vSyncNumerator;
        public uint vSyncDenominator;
        public uint activeWidth;
        public uint activeHeight;
        public uint totalWidth;
        public uint totalHeight;
        public uint videoStandard;
        public uint scanLineOrdering;
    }

    // DISPLAYCONFIG_SOURCE_MODE (20 bytes): the source-mode arm of the union.
    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_SOURCE_MODE
    {
        public uint width;
        public uint height;
        public uint pixelFormat;
        public int positionX;
        public int positionY;
    }

    // 64 bytes: infoType, id, adapterId, then a 48-byte, 8-aligned union.
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    private struct DISPLAYCONFIG_MODE_INFO
    {
        [FieldOffset(0)] public uint infoType;
        [FieldOffset(4)] public uint id;
        [FieldOffset(8)] public LUID adapterId;
        [FieldOffset(16)] public DISPLAYCONFIG_VIDEO_SIGNAL_INFO targetMode;
        [FieldOffset(16)] public DISPLAYCONFIG_SOURCE_MODE sourceMode;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_DEVICE_INFO_HEADER
    {
        public uint type;
        public uint size;
        public LUID adapterId;
        public uint id;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAYCONFIG_TARGET_DEVICE_NAME
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        public uint flags;
        public uint outputTechnology;
        public ushort edidManufactureId;
        public ushort edidProductCodeId;
        public uint connectorInstance;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string monitorFriendlyDeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string monitorDevicePath;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(nint hdc, nint lprcClip, MonitorEnumProc lpfnEnum, nint dwData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFOEX lpmi);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplaySettings(string? lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetDisplayConfigBufferSizes(uint flags, out uint numPathArrayElements, out uint numModeInfoArrayElements);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int QueryDisplayConfig(uint flags, ref uint numPathArrayElements, [Out] DISPLAYCONFIG_PATH_INFO[] pathArray, ref uint numModeInfoArrayElements, [Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray, nint currentTopologyId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_TARGET_DEVICE_NAME requestPacket);
}
