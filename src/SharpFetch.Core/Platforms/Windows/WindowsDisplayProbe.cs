using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;
using SharpFetch.Platforms.Common;

namespace SharpFetch.Platforms.Windows;

[SupportedOSPlatform("windows")]
public sealed class WindowsDisplayProbe : IDisplayProbe
{
    public IReadOnlyList<DisplayInfo> DetectDisplays()
    {
        var results = new List<DisplayInfo>();

        if (!OperatingSystem.IsWindows())
            return results;

        try
        {
            // 1. Gather all EDID info from the Registry
            var edidList = LoadAllEdidsFromRegistry();

            // 2. Query active display paths and output technology via QueryDisplayConfig
            var pathConfig = QueryActiveDisplayPaths();

            // 3. Enumerate all active screens via EnumDisplayMonitors
            EnumDisplayMonitors(nint.Zero, nint.Zero, (nint hMonitor, nint hdcMonitor, ref RECT lprcMonitor, nint dwData) =>
            {
                var mi = new MONITORINFOEX();
                mi.cbSize = Marshal.SizeOf<MONITORINFOEX>();

                if (GetMonitorInfo(hMonitor, ref mi))
                {
                    int width = mi.rcMonitor.right - mi.rcMonitor.left;
                    int height = mi.rcMonitor.bottom - mi.rcMonitor.top;
                    bool isPrimary = (mi.dwFlags & 1) != 0;

                    // Query refresh rate via EnumDisplaySettings
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

                    // Match EDID and DisplayConfig info
                    int index = results.Count;
                    string name = "Display";
                    int? inches = null;
                    DisplayType displayType = DisplayType.External;

                    if (index < pathConfig.Count)
                    {
                        var config = pathConfig[index];
                        if (!string.IsNullOrEmpty(config.Name))
                            name = config.Name;

                        displayType = config.Type;
                    }

                    // Try to match from Registry EDID list
                    if (index < edidList.Count)
                    {
                        var edid = edidList[index];
                        if (!string.IsNullOrEmpty(edid.Name))
                            name = edid.Name;

                        if (edid.Inches.HasValue)
                            inches = edid.Inches;
                    }
                    else if (edidList.Count > 0 && inches == null)
                    {
                        // Match first available EDID with inches
                        var edid = edidList.Find(e => e.Inches.HasValue);
                        if (edid.Inches.HasValue)
                        {
                            if (!string.IsNullOrEmpty(edid.Name) && name == "Display")
                                name = edid.Name;
                            inches = edid.Inches;
                        }
                    }

                    results.Add(new DisplayInfo
                    {
                        Name = name,
                        Width = width,
                        Height = height,
                        RefreshRate = refreshRate,
                        DiagonalInches = inches,
                        Type = displayType,
                        IsPrimary = isPrimary
                    });
                }
                return true;
            }, nint.Zero);
        }
        catch
        {
            // Fallback
        }

        return results;
    }

    private static List<(string? Name, int? Inches)> LoadAllEdidsFromRegistry()
    {
        var list = new List<(string? Name, int? Inches)>();
        try
        {
            using var baseKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\DISPLAY");
            if (baseKey == null) return list;

            foreach (string hwId in baseKey.GetSubKeyNames())
            {
                using var hwKey = baseKey.OpenSubKey(hwId);
                if (hwKey == null) continue;

                foreach (string instance in hwKey.GetSubKeyNames())
                {
                    using var devParams = hwKey.OpenSubKey($@"{instance}\Device Parameters");
                    if (devParams == null) continue;

                    if (devParams.GetValue("EDID") is byte[] edidBytes && edidBytes.Length >= 128)
                    {
                        if (EdidParser.TryParse(edidBytes, out string? name, out int? inches))
                        {
                            list.Add((name, inches));
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore registry permission errors
        }

        return list;
    }

    private static List<(string? Name, DisplayType Type)> QueryActiveDisplayPaths()
    {
        var list = new List<(string? Name, DisplayType Type)>();

        try
        {
            if (GetDisplayConfigBufferSizes(2 /* QDC_ONLY_ACTIVE_PATHS */, out uint pathCount, out uint modeCount) != 0)
                return list;

            var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
            var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];

            if (QueryDisplayConfig(2, ref pathCount, paths, ref modeCount, modes, nint.Zero) != 0)
                return list;

            for (int i = 0; i < pathCount; i++)
            {
                var p = paths[i];
                var targetName = new DISPLAYCONFIG_TARGET_DEVICE_NAME();
                targetName.header.type = 2; // DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME
                targetName.header.size = (uint)Marshal.SizeOf<DISPLAYCONFIG_TARGET_DEVICE_NAME>();
                targetName.header.adapterId = p.targetInfo.adapterId;
                targetName.header.id = p.targetInfo.id;

                string? friendlyName = null;
                if (DisplayConfigGetDeviceInfo(ref targetName) == 0)
                {
                    if (!string.IsNullOrWhiteSpace(targetName.monitorFriendlyDeviceName))
                        friendlyName = targetName.monitorFriendlyDeviceName.Trim();
                }

                // Output technologies: 0x80000000 (Internal), 11 (DisplayPort Embedded), 13 (UDI Embedded) -> Built-in
                uint tech = p.targetInfo.outputTechnology;
                bool isBuiltin = tech is 0x80000000 or 11 or 13;
                DisplayType type = isBuiltin ? DisplayType.Builtin : DisplayType.External;

                list.Add((friendlyName, type));
            }
        }
        catch
        {
            // Ignore
        }

        return list;
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
        public bool targetAvailable;
        public uint statusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_PATH_INFO
    {
        public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
        public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
        public uint flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_MODE_INFO
    {
        public uint infoType;
        public uint id;
        public LUID adapterId;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] modeInfo;
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
