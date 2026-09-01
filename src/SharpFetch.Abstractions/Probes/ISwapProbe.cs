using SharpFetch.Core.Models;

namespace SharpFetch.Core.Probes;

public interface ISwapProbe
{
    SwapInfo Detect();
}
