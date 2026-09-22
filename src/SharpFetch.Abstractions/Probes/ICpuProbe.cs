using SharpFetch.Core.Models;

namespace SharpFetch.Core.Probes;

public interface ICpuProbe
{
    CpuInfo Detect();
}
