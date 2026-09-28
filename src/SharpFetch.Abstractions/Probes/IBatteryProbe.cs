using SharpFetch.Core.Models;

namespace SharpFetch.Core.Probes;

public interface IBatteryProbe
{
    IReadOnlyList<BatteryInfo> DetectBatteries();
}
