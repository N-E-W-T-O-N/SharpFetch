using SharpFetch.Core.Models;

namespace SharpFetch.Core.Probes;

public interface IHostProbe
{
    HostInfo? Detect();
}
