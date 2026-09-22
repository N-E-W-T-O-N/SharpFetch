using SharpFetch.Core.Models;

namespace SharpFetch.Core.Probes;

public interface INetworkProbe
{
    IReadOnlyList<NetworkInterfaceInfo> DetectInterfaces();
}
