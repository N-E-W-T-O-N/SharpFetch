using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms.Common;

public sealed class FallbackDisplayProbe : IDisplayProbe
{
    public IReadOnlyList<DisplayInfo> DetectDisplays() => Array.Empty<DisplayInfo>();
}
