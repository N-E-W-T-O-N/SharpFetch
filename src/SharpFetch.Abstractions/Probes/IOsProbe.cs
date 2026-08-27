using SharpFetch.Core.Models;

namespace SharpFetch.Core.Probes;

public interface IOsProbe
{
    OsInfo Detect();
}
