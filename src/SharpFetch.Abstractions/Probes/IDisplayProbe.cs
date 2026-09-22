using SharpFetch.Core.Models;

namespace SharpFetch.Core.Probes;

public interface IDisplayProbe
{
    IReadOnlyList<DisplayInfo> DetectDisplays();
}
