using System.Text;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Modules;

public sealed class DisplayModule : IFetchModule
{
    private readonly IDisplayProbe _probe;

    public DisplayModule(IDisplayProbe? probe = null)
    {
        _probe = probe ?? DisplayProbeFactory.Create();
    }

    public ModuleMetadata Metadata => new(
        Key: "Display",
        DisplayName: "Display",
        Description: "Print resolutions, refresh rates, physical sizes, etc.",
        DefaultOrder: 21,
        Icon: "󰍹"
    );

    public bool IsSupported => true;

    public IReadOnlyList<ModuleResult> Fetch()
    {
        var displays = _probe.DetectDisplays();
        if (displays.Count == 0)
            return Array.Empty<ModuleResult>();

        var results = new List<ModuleResult>();

        for (int i = 0; i < displays.Count; i++)
        {
            var d = displays[i];
            var sb = new StringBuilder();

            // e.g. "1600x900 in 20", 60 Hz [External]"
            sb.Append($"{d.Width}x{d.Height}");

            if (d.DiagonalInches.HasValue && d.DiagonalInches.Value > 0)
            {
                sb.Append($" in {d.DiagonalInches.Value}\"");
            }

            if (d.RefreshRate > 0)
            {
                sb.Append($", {Math.Round(d.RefreshRate)} Hz");
            }

            if (d.Type != DisplayType.Unknown)
            {
                string typeStr = d.Type == DisplayType.Builtin ? "[Built-in]" : "[External]";
                sb.Append($" {typeStr}");
            }

            if (displays.Count > 1 && d.IsPrimary)
            {
                sb.Append(" *");
            }

            string displayName = !string.IsNullOrEmpty(d.Name) && d.Name != "Display"
                ? $"Display ({d.Name})"
                : (displays.Count == 1 ? "Display" : $"Display ({i + 1})");

            results.Add(new ModuleResult(
                Key: $"Display_{i}",
                DisplayName: displayName,
                FormattedValue: sb.ToString(),
                RawData: d
            ));
        }

        return results;
    }
}
