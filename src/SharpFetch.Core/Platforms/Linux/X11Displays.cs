using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using SharpFetch.Core.Models;
using SharpFetch.Core.Platforms.Common;

namespace SharpFetch.Core.Platforms.Linux;

/// <summary>
/// Lists the X server's active monitors through XRandR 1.5 (XRRGetMonitors),
/// the same source fastfetch's x11 display backend uses. Unlike the DRM sysfs
/// connectors, this reports what the desktop is actually driving - current
/// resolution and refresh rate, monitors on a GPU whose driver exposes no DRM
/// connectors (NVIDIA without nvidia-drm.modeset), and only enabled outputs.
/// libX11/libXrandr are loaded at runtime, so headless systems without them
/// simply fall back to DRM.
/// </summary>
[SupportedOSPlatform("linux")]
internal static unsafe class X11Displays
{
    private const int RrInterlace = 0x10;
    private const int RrDoubleScan = 0x20;

    /// <summary>Returns null when no X server or XRandR is reachable.</summary>
    public static List<DisplayInfo>? TryDetect()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")))
            return null;

        if (!NativeLibrary.TryLoad("libX11.so.6", out nint x11) ||
            !NativeLibrary.TryLoad("libXrandr.so.2", out nint xrandr))
            return null;

        try
        {
            var api = new Api(x11, xrandr);
            return api.IsComplete ? Detect(api) : null;
        }
        catch
        {
            return null;
        }
    }

    private static List<DisplayInfo>? Detect(Api api)
    {
        nint display = api.XOpenDisplay(null);
        if (display == 0)
            return null;

        // Xlib's default error handler exits the process; a stale output or
        // CRTC id racing a hotplug must only skip that monitor.
        delegate* unmanaged<nint, nint, int> previousHandler = api.XSetErrorHandler(&IgnoreXError);
        try
        {
            nuint root = api.XDefaultRootWindow(display);
            int count;
            XRRMonitorInfo* monitors = api.XRRGetMonitors(display, root, 1, &count);
            if (monitors == null)
                return null;

            XRRScreenResources* resources = api.XRRGetScreenResourcesCurrent(display, root);
            nuint edidAtom = InternAtom(api, display, "EDID");

            var results = new List<DisplayInfo>(count);
            try
            {
                for (int i = 0; i < count; i++)
                {
                    DisplayInfo? info = ReadMonitor(api, display, resources, edidAtom, monitors[i]);
                    if (info is not null)
                        results.Add(info);
                }
            }
            finally
            {
                if (resources != null) api.XRRFreeScreenResources(resources);
                api.XRRFreeMonitors(monitors);
            }

            if (results.Count > 0 && !results.Exists(r => r.IsPrimary))
                results[0] = results[0] with { IsPrimary = true };

            return results;
        }
        finally
        {
            api.XSetErrorHandler(previousHandler);
            api.XCloseDisplay(display);
        }
    }

    private static DisplayInfo? ReadMonitor(Api api, nint display, XRRScreenResources* resources, nuint edidAtom, XRRMonitorInfo monitor)
    {
        if (monitor.width <= 0 || monitor.height <= 0)
            return null;

        string? outputName = null;
        string? edidName = null;
        int? inches = null;
        double refreshRate = 0;

        if (monitor.noutput > 0 && monitor.outputs != null && resources != null)
        {
            nuint output = monitor.outputs[0];
            XRROutputInfo* outputInfo = api.XRRGetOutputInfo(display, resources, output);
            if (outputInfo != null)
            {
                if (outputInfo->name != null && outputInfo->nameLen > 0)
                    outputName = Encoding.UTF8.GetString(outputInfo->name, outputInfo->nameLen);

                if (outputInfo->crtc != 0)
                    refreshRate = ReadRefreshRate(api, display, resources, outputInfo->crtc);

                api.XRRFreeOutputInfo(outputInfo);
            }

            if (edidAtom != 0 && TryReadEdid(api, display, output, edidAtom, out byte[]? edid) &&
                EdidParser.TryParse(edid, out string? parsedName, out int? parsedInches))
            {
                edidName = parsedName;
                inches = parsedInches;
            }
        }

        outputName ??= AtomName(api, display, monitor.name);
        inches ??= DiagonalInches(monitor.mwidth, monitor.mheight);

        return new DisplayInfo
        {
            Name = !string.IsNullOrEmpty(edidName) ? edidName : outputName ?? "Display",
            Width = monitor.width,
            Height = monitor.height,
            RefreshRate = refreshRate,
            DiagonalInches = inches,
            Type = ClassifyOutput(outputName),
            IsPrimary = monitor.primary != 0
        };
    }

    private static double ReadRefreshRate(Api api, nint display, XRRScreenResources* resources, nuint crtc)
    {
        XRRCrtcInfo* crtcInfo = api.XRRGetCrtcInfo(display, resources, crtc);
        if (crtcInfo == null)
            return 0;

        nuint modeId = crtcInfo->mode;
        api.XRRFreeCrtcInfo(crtcInfo);

        for (int m = 0; m < resources->nmode; m++)
        {
            XRRModeInfo mode = resources->modes[m];
            if (mode.id == modeId)
                return RefreshRate(mode.dotClock, mode.hTotal, mode.vTotal, (ulong)mode.modeFlags);
        }

        return 0;
    }

    /// <summary>dotClock / (hTotal * vTotal), corrected for interlace and doublescan, as xrandr computes it.</summary>
    internal static double RefreshRate(nuint dotClock, uint hTotal, uint vTotal, ulong modeFlags)
    {
        double v = vTotal;
        if ((modeFlags & RrDoubleScan) != 0) v *= 2;
        if ((modeFlags & RrInterlace) != 0) v /= 2;

        return hTotal == 0 || v == 0 ? 0 : Math.Round(dotClock / (hTotal * v), 2);
    }

    /// <summary>eDP/LVDS/DSI are laptop and tablet panels; anything else is an external monitor.</summary>
    internal static DisplayType ClassifyOutput(string? outputName)
    {
        if (string.IsNullOrEmpty(outputName))
            return DisplayType.Unknown;

        return outputName.StartsWith("eDP", StringComparison.OrdinalIgnoreCase) ||
               outputName.StartsWith("LVDS", StringComparison.OrdinalIgnoreCase) ||
               outputName.StartsWith("DSI", StringComparison.OrdinalIgnoreCase)
            ? DisplayType.Builtin
            : DisplayType.External;
    }

    internal static int? DiagonalInches(int widthMm, int heightMm) =>
        widthMm > 0 && heightMm > 0
            ? (int)Math.Round(Math.Sqrt((double)widthMm * widthMm + (double)heightMm * heightMm) / 25.4)
            : null;

    private static bool TryReadEdid(Api api, nint display, nuint output, nuint edidAtom, out byte[]? edid)
    {
        edid = null;
        nuint actualType, nitems, bytesAfter;
        int actualFormat;
        byte* data = null;

        // AnyPropertyType (0); 128 32-bit units covers EDID's base block plus extensions.
        int status = api.XRRGetOutputProperty(display, output, edidAtom, 0, 128, 0, 0, 0,
            &actualType, &actualFormat, &nitems, &bytesAfter, &data);
        try
        {
            if (status != 0 || data == null || actualFormat != 8 || nitems < 128)
                return false;

            edid = new ReadOnlySpan<byte>(data, (int)nitems).ToArray();
            return true;
        }
        finally
        {
            if (data != null) api.XFree(data);
        }
    }

    private static nuint InternAtom(Api api, nint display, string name)
    {
        byte* bytes = stackalloc byte[name.Length + 1];
        Encoding.ASCII.GetBytes(name, new Span<byte>(bytes, name.Length));
        bytes[name.Length] = 0;
        return api.XInternAtom(display, bytes, 1 /* only_if_exists */);
    }

    private static string? AtomName(Api api, nint display, nuint atom)
    {
        if (atom == 0)
            return null;

        byte* name = api.XGetAtomName(display, atom);
        if (name == null)
            return null;

        try
        {
            return Marshal.PtrToStringUTF8((nint)name);
        }
        finally
        {
            api.XFree(name);
        }
    }

    [UnmanagedCallersOnly]
    private static int IgnoreXError(nint display, nint errorEvent) => 0;

    /// <summary>Function pointers resolved from libX11/libXrandr - no runtime marshalling, so Native AOT safe.</summary>
    private sealed class Api
    {
        public readonly delegate* unmanaged<byte*, nint> XOpenDisplay;
        public readonly delegate* unmanaged<nint, int> XCloseDisplay;
        public readonly delegate* unmanaged<nint, nuint> XDefaultRootWindow;
        public readonly delegate* unmanaged<void*, int> XFree;
        public readonly delegate* unmanaged<nint, nuint, byte*> XGetAtomName;
        public readonly delegate* unmanaged<nint, byte*, int, nuint> XInternAtom;
        public readonly delegate* unmanaged<delegate* unmanaged<nint, nint, int>, delegate* unmanaged<nint, nint, int>> XSetErrorHandler;
        public readonly delegate* unmanaged<nint, nuint, int, int*, XRRMonitorInfo*> XRRGetMonitors;
        public readonly delegate* unmanaged<XRRMonitorInfo*, void> XRRFreeMonitors;
        public readonly delegate* unmanaged<nint, nuint, XRRScreenResources*> XRRGetScreenResourcesCurrent;
        public readonly delegate* unmanaged<XRRScreenResources*, void> XRRFreeScreenResources;
        public readonly delegate* unmanaged<nint, XRRScreenResources*, nuint, XRROutputInfo*> XRRGetOutputInfo;
        public readonly delegate* unmanaged<XRROutputInfo*, void> XRRFreeOutputInfo;
        public readonly delegate* unmanaged<nint, XRRScreenResources*, nuint, XRRCrtcInfo*> XRRGetCrtcInfo;
        public readonly delegate* unmanaged<XRRCrtcInfo*, void> XRRFreeCrtcInfo;
        public readonly delegate* unmanaged<nint, nuint, nuint, nint, nint, int, int, nuint, nuint*, int*, nuint*, nuint*, byte**, int> XRRGetOutputProperty;

        public Api(nint x11, nint xrandr)
        {
            XOpenDisplay = (delegate* unmanaged<byte*, nint>)Get(x11, "XOpenDisplay");
            XCloseDisplay = (delegate* unmanaged<nint, int>)Get(x11, "XCloseDisplay");
            XDefaultRootWindow = (delegate* unmanaged<nint, nuint>)Get(x11, "XDefaultRootWindow");
            XFree = (delegate* unmanaged<void*, int>)Get(x11, "XFree");
            XGetAtomName = (delegate* unmanaged<nint, nuint, byte*>)Get(x11, "XGetAtomName");
            XInternAtom = (delegate* unmanaged<nint, byte*, int, nuint>)Get(x11, "XInternAtom");
            XSetErrorHandler = (delegate* unmanaged<delegate* unmanaged<nint, nint, int>, delegate* unmanaged<nint, nint, int>>)Get(x11, "XSetErrorHandler");
            XRRGetMonitors = (delegate* unmanaged<nint, nuint, int, int*, XRRMonitorInfo*>)Get(xrandr, "XRRGetMonitors");
            XRRFreeMonitors = (delegate* unmanaged<XRRMonitorInfo*, void>)Get(xrandr, "XRRFreeMonitors");
            XRRGetScreenResourcesCurrent = (delegate* unmanaged<nint, nuint, XRRScreenResources*>)Get(xrandr, "XRRGetScreenResourcesCurrent");
            XRRFreeScreenResources = (delegate* unmanaged<XRRScreenResources*, void>)Get(xrandr, "XRRFreeScreenResources");
            XRRGetOutputInfo = (delegate* unmanaged<nint, XRRScreenResources*, nuint, XRROutputInfo*>)Get(xrandr, "XRRGetOutputInfo");
            XRRFreeOutputInfo = (delegate* unmanaged<XRROutputInfo*, void>)Get(xrandr, "XRRFreeOutputInfo");
            XRRGetCrtcInfo = (delegate* unmanaged<nint, XRRScreenResources*, nuint, XRRCrtcInfo*>)Get(xrandr, "XRRGetCrtcInfo");
            XRRFreeCrtcInfo = (delegate* unmanaged<XRRCrtcInfo*, void>)Get(xrandr, "XRRFreeCrtcInfo");
            XRRGetOutputProperty = (delegate* unmanaged<nint, nuint, nuint, nint, nint, int, int, nuint, nuint*, int*, nuint*, nuint*, byte**, int>)Get(xrandr, "XRRGetOutputProperty");

            IsComplete = XOpenDisplay != null && XCloseDisplay != null && XDefaultRootWindow != null && XFree != null &&
                         XGetAtomName != null && XInternAtom != null && XSetErrorHandler != null &&
                         XRRGetMonitors != null && XRRFreeMonitors != null &&
                         XRRGetScreenResourcesCurrent != null && XRRFreeScreenResources != null &&
                         XRRGetOutputInfo != null && XRRFreeOutputInfo != null &&
                         XRRGetCrtcInfo != null && XRRFreeCrtcInfo != null && XRRGetOutputProperty != null;
        }

        public bool IsComplete { get; }

        private static void* Get(nint library, string name) =>
            NativeLibrary.TryGetExport(library, name, out nint address) ? (void*)address : null;
    }

    // Xlib/XRandR structs. C `unsigned long` (Time, Atom, XID, RRMode) is
    // nuint so the layouts hold on both LP64 and 32-bit ARM.
    [StructLayout(LayoutKind.Sequential)]
    private struct XRRMonitorInfo
    {
        public nuint name;
        public int primary;
        public int automatic;
        public int noutput;
        public int x, y, width, height;
        public int mwidth, mheight;
        public nuint* outputs;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XRRScreenResources
    {
        public nuint timestamp;
        public nuint configTimestamp;
        public int ncrtc;
        public nuint* crtcs;
        public int noutput;
        public nuint* outputs;
        public int nmode;
        public XRRModeInfo* modes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XRRModeInfo
    {
        public nuint id;
        public uint width, height;
        public nuint dotClock;
        public uint hSyncStart, hSyncEnd, hTotal, hSkew;
        public uint vSyncStart, vSyncEnd, vTotal;
        public byte* name;
        public uint nameLength;
        public nuint modeFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XRROutputInfo
    {
        public nuint timestamp;
        public nuint crtc;
        public byte* name;
        public int nameLen;
        public nuint mm_width;
        public nuint mm_height;
        public ushort connection;
        public ushort subpixel_order;
        public int ncrtc;
        public nuint* crtcs;
        public int nclone;
        public nuint* clones;
        public int nmode;
        public int npreferred;
        public nuint* modes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XRRCrtcInfo
    {
        public nuint timestamp;
        public int x, y;
        public uint width, height;
        public nuint mode;
        public ushort rotation;
        public int noutput;
        public nuint* outputs;
        public ushort rotations;
        public int npossible;
        public nuint* possible;
    }
}
