using System.Runtime.Versioning;
using SharpFetch.Core.Models;
using SharpFetch.Core.Platforms.Common;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms.Linux;

/// <summary>
/// Combines two sources, like fastfetch: the X server's active monitors
/// (XRandR, also served by XWayland under Wayland) and the kernel's DRM
/// connectors in sysfs. X11 knows the current mode and every monitor the
/// desktop drives; DRM works without a display server and has the panel's
/// real connector name and EDID. See <see cref="Choose"/> for how they merge.
/// </summary>
[SupportedOSPlatform("linux")]
public sealed class LinuxDisplayProbe : IDisplayProbe
{
    public IReadOnlyList<DisplayInfo> DetectDisplays() =>
        Choose(X11Displays.TryDetect(), DetectFromDrm());

    /// <summary>
    /// Prefers X11 - it reports the current resolution and refresh rate and
    /// sees monitors on drivers without DRM connectors. Under XWayland the
    /// outputs are synthetic ("XWAYLAND0") with no EDID or panel type, so DRM
    /// wins there unless it found fewer displays than the compositor drives.
    /// </summary>
    internal static IReadOnlyList<DisplayInfo> Choose(IReadOnlyList<DisplayInfo>? x11, IReadOnlyList<DisplayInfo> drm)
    {
        if (x11 is null || x11.Count == 0)
            return drm;

        bool isXWayland = x11.All(d => d.Name.StartsWith("XWAYLAND", StringComparison.OrdinalIgnoreCase));
        return isXWayland && drm.Count >= x11.Count ? drm : x11;
    }

    private static List<DisplayInfo> DetectFromDrm()
    {
        var results = new List<DisplayInfo>();
        // Connected-but-disabled outputs (a monitor plugged in but turned off
        // in display settings) are kept aside and only used if nothing is enabled.
        var disabled = new List<DisplayInfo>();

        if (!Directory.Exists("/sys/class/drm"))
            return results;

        string[] connectorDirs;
        try
        {
            connectorDirs = Directory.GetDirectories("/sys/class/drm", "card*-*");
        }
        catch
        {
            return results;
        }

        foreach (string connectorDir in connectorDirs)
        {
            // Each connector is read independently - a permission error or
            // transient I/O failure on one sysfs entry must not abort the
            // whole scan and drop every other already/still-readable display.
            try
            {
                string statusPath = Path.Combine(connectorDir, "status");
                if (!File.Exists(statusPath) || File.ReadAllText(statusPath).Trim() != "connected")
                    continue;

                string connectorName = Path.GetFileName(connectorDir);
                int dashIdx = connectorName.IndexOf('-');
                string portName = dashIdx >= 0 ? connectorName[(dashIdx + 1)..] : connectorName;

                // Resolution
                int width = 0, height = 0;
                string modesPath = Path.Combine(connectorDir, "modes");
                if (File.Exists(modesPath))
                {
                    string firstMode = File.ReadLines(modesPath).FirstOrDefault() ?? "";
                    string[] dim = firstMode.Split('x');
                    if (dim.Length == 2 && int.TryParse(dim[0], out int w) && int.TryParse(dim[1], out int h))
                    {
                        width = w;
                        height = h;
                    }
                }

                if (width == 0 || height == 0)
                    continue;

                string enabledPath = Path.Combine(connectorDir, "enabled");
                bool isEnabled = !File.Exists(enabledPath) || File.ReadAllText(enabledPath).Trim() == "enabled";

                // Builtin vs External
                bool isBuiltin = portName.StartsWith("eDP", StringComparison.OrdinalIgnoreCase) ||
                                 portName.StartsWith("LVDS", StringComparison.OrdinalIgnoreCase);
                var displayType = isBuiltin ? DisplayType.Builtin : DisplayType.External;

                // EDID
                string name = portName;
                int? inches = null;
                string edidPath = Path.Combine(connectorDir, "edid");
                if (File.Exists(edidPath))
                {
                    byte[] edidBytes = File.ReadAllBytes(edidPath);
                    if (EdidParser.TryParse(edidBytes, out string? edidName, out int? edidInches))
                    {
                        if (!string.IsNullOrEmpty(edidName))
                            name = edidName;

                        inches = edidInches;
                    }
                }

                (isEnabled ? results : disabled).Add(new DisplayInfo
                {
                    Name = name,
                    Width = width,
                    Height = height,
                    RefreshRate = 60.0,
                    DiagonalInches = inches,
                    Type = displayType
                });
            }
            catch
            {
                // Skip this connector only; keep scanning the rest.
            }
        }

        if (results.Count == 0)
            results = disabled;

        if (results.Count > 0)
            results[0] = results[0] with { IsPrimary = true };

        return results;
    }
}
