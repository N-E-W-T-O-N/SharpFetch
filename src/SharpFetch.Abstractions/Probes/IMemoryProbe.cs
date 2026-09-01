using SharpFetch.Core.Models;

namespace SharpFetch.Core.Probes;

public interface IMemoryProbe
{
    MemoryInfo Detect();
}
