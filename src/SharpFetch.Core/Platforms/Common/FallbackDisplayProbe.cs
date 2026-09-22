using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Platforms.Common;

public sealed class FallbackDisplayProbe : IDisplayProbe
{
    public IReadOnlyList<DisplayInfo> DetectDisplays() => Array.Empty<DisplayInfo>();
}
