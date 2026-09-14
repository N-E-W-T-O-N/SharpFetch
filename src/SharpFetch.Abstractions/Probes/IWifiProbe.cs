using SharpFetch.Core.Models;

namespace SharpFetch.Core.Probes;

public interface IWifiProbe
{
    IReadOnlyList<WifiConnectionInfo> DetectWifi();
}
