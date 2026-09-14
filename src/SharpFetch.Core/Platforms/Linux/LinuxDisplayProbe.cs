using System.Runtime.Versioning;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;
using SharpFetch.Platforms.Common;

namespace SharpFetch.Platforms.Linux;

[SupportedOSPlatform("linux")]
public sealed class LinuxDisplayProbe : IDisplayProbe
{
    public IReadOnlyList<DisplayInfo> DetectDisplays()
    {
        var results = new List<DisplayInfo>();

        if (!Directory.Exists("/sys/class/drm"))
            return results;

        try
        {
            foreach (string connectorDir in Directory.GetDirectories("/sys/class/drm", "card*-*"))
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

                results.Add(new DisplayInfo
                {
                    Name = name,
                    Width = width,
                    Height = height,
                    RefreshRate = 60.0,
                    DiagonalInches = inches,
                    Type = displayType,
                    IsPrimary = results.Count == 0
                });
            }
        }
        catch
        {
            // Ignore
        }

        return results;
    }
}
