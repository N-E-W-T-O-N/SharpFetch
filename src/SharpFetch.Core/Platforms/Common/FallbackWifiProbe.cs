using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Platforms.Common;

public sealed class FallbackWifiProbe : IWifiProbe
{
    public IReadOnlyList<WifiConnectionInfo> DetectWifi() => Array.Empty<WifiConnectionInfo>();
}
