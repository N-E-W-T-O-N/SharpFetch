using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Platforms.Windows;

[SupportedOSPlatform("windows")]
public sealed partial class WindowsWifiProbe : IWifiProbe
{
    public IReadOnlyList<WifiConnectionInfo> DetectWifi()
    {
        var results = new List<WifiConnectionInfo>();

        if (!OperatingSystem.IsWindows())
            return results;

        nint clientHandle = nint.Zero;
        nint ifListPtr = nint.Zero;

        try
        {
            uint error = WlanOpenHandle(2, nint.Zero, out _, out clientHandle);
            if (error != 0 || clientHandle == nint.Zero)
                return results;

            error = WlanEnumInterfaces(clientHandle, nint.Zero, out ifListPtr);
            if (error != 0 || ifListPtr == nint.Zero)
                return results;

            int count = Marshal.ReadInt32(ifListPtr, 0);
            nint currentPtr = ifListPtr + 8; // Skip dwNumberOfItems (4) and dwIndex (4)

            for (int i = 0; i < count; i++)
            {
                // Each WLAN_INTERFACE_INFO is: Guid (16 bytes) + Description (512 bytes) + State (4 bytes) = 532 bytes
                byte[] guidBytes = new byte[16];
                Marshal.Copy(currentPtr, guidBytes, 0, 16);
                var interfaceGuid = new Guid(guidBytes);

                string desc = Marshal.PtrToStringUni(currentPtr + 16, 256) ?? "Wi-Fi";
                int nullIdx = desc.IndexOf('\0');
                if (nullIdx >= 0) desc = desc[..nullIdx];

                int state = Marshal.ReadInt32(currentPtr + 16 + 512);

                // wlan_interface_state_connected = 1
                if (state == 1)
                {
                    var connInfo = QueryConnection(clientHandle, interfaceGuid, desc);
                    if (connInfo != null)
                    {
                        results.Add(connInfo);
                    }
                }

                currentPtr += 532;
            }
        }
        catch
        {
            // WLAN service disabled or not present
        }
        finally
        {
            if (ifListPtr != nint.Zero)
                WlanFreeMemory(ifListPtr);

            if (clientHandle != nint.Zero)
                WlanCloseHandle(clientHandle, nint.Zero);
        }

        return results;
    }

    private static WifiConnectionInfo? QueryConnection(nint clientHandle, Guid interfaceGuid, string interfaceName)
    {
        const uint wlanIntfOpcodeCurrentConnection = 7;
        // WLAN_CONNECTION_ATTRIBUTES: the last field this method reads is the
        // security attributes' authAlgorithm at assocPtr+68+8, i.e. 520+68+8+4
        // bytes into the structure - require at least that much before trusting
        // any fixed offset into it, rather than assuming the driver always
        // returns the full documented layout.
        const uint minDataSize = 520 + 68 + 8 + 4;
        nint dataPtr = nint.Zero;

        try
        {
            uint error = WlanQueryInterface(clientHandle, ref interfaceGuid, wlanIntfOpcodeCurrentConnection, nint.Zero, out uint dataSize, out dataPtr, out _);
            if (error != 0 || dataPtr == nint.Zero || dataSize < minDataSize)
                return null;

            // WLAN_CONNECTION_ATTRIBUTES structure layout
            // Read WLAN_ASSOCIATION_ATTRIBUTES:
            // dot11Ssid at offset: state(4) + mode(4) + profile(512) = offset 520
            nint assocPtr = dataPtr + 520;

            uint ssidLen = (uint)Marshal.ReadInt32(assocPtr, 0);
            if (ssidLen is 0 or > 32)
                return null;

            byte[] ssidBytes = new byte[ssidLen];
            Marshal.Copy(assocPtr + 4, ssidBytes, 0, (int)ssidLen);
            string ssid = Encoding.UTF8.GetString(ssidBytes);

            // BSSID (MAC of Access Point) at offset 40 in WLAN_ASSOCIATION_ATTRIBUTES
            byte[] bssidBytes = new byte[6];
            Marshal.Copy(assocPtr + 40, bssidBytes, 0, 6);
            string bssid = string.Join(":", bssidBytes.Select(b => b.ToString("X2")));

            // dot11PhyType at offset 48
            int phyType = Marshal.ReadInt32(assocPtr + 48);
            string protocol = MapPhyType(phyType);

            // wlanSignalQuality at offset 56 (ULONG 0-100)
            uint signalQuality = (uint)Marshal.ReadInt32(assocPtr + 56);

            // Rx/Tx rate at offset 60, 64 (in Kbps)
            uint rxRate = (uint)Marshal.ReadInt32(assocPtr + 60);
            uint txRate = (uint)Marshal.ReadInt32(assocPtr + 64);

            // WLAN_SECURITY_ATTRIBUTES at offset 520 + 68 = 588
            nint secPtr = assocPtr + 68;
            int authAlgo = Marshal.ReadInt32(secPtr + 8);
            string security = MapAuthAlgorithm(authAlgo);

            return new WifiConnectionInfo
            {
                InterfaceName = interfaceName,
                Ssid = ssid,
                Bssid = bssid,
                SignalQualityPercent = (int)Math.Clamp(signalQuality, 0, 100),
                Protocol = protocol,
                Security = security,
                RxRateMbps = rxRate > 0 ? rxRate / 1000.0 : null,
                TxRateMbps = txRate > 0 ? txRate / 1000.0 : null
            };
        }
        catch
        {
            return null;
        }
        finally
        {
            if (dataPtr != nint.Zero)
                WlanFreeMemory(dataPtr);
        }
    }

    private static string MapPhyType(int phyType) => phyType switch
    {
        1 => "802.11 (FHSS)",
        2 => "802.11 (DSSS)",
        3 => "802.11 (IR)",
        4 => "802.11a",
        5 => "802.11b",
        6 => "802.11g",
        7 => "Wi-Fi 4 (802.11n)",
        8 => "Wi-Fi 5 (802.11ac)",
        9 => "802.11ad (WiGig)",
        10 => "Wi-Fi 6 (802.11ax)",
        11 => "Wi-Fi 7 (802.11be)",
        _ => "802.11"
    };

    private static string MapAuthAlgorithm(int algo) => algo switch
    {
        1 => "Open",
        2 => "Shared",
        3 => "WPA-Enterprise",
        4 => "WPA-Personal",
        5 => "WPA-None",
        6 => "WPA2-Enterprise",
        7 => "WPA2-Personal",
        8 => "WPA3-Enterprise",
        9 => "WPA3-Personal",
        _ => "WPA/WPA2"
    };

    [LibraryImport("wlanapi.dll")]
    private static partial uint WlanOpenHandle(uint dwClientVersion, nint pReserved, out uint pdwNegotiatedVersion, out nint phClientHandle);

    [LibraryImport("wlanapi.dll")]
    private static partial uint WlanCloseHandle(nint hClientHandle, nint pReserved);

    [LibraryImport("wlanapi.dll")]
    private static partial uint WlanEnumInterfaces(nint hClientHandle, nint pReserved, out nint ppInterfaceList);

    [LibraryImport("wlanapi.dll")]
    private static partial uint WlanQueryInterface(nint hClientHandle, ref Guid pInterfaceGuid, uint opCode, nint pReserved, out uint pdwDataSize, out nint ppData, out uint pWlanOpcodeValueType);

    [LibraryImport("wlanapi.dll")]
    private static partial void WlanFreeMemory(nint pMemory);
}
