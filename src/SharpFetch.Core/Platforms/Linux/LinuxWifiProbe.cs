using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Platforms.Linux;

/// <summary>
/// Enumerates wireless interfaces and signal quality from /proc/net/wireless
/// (plain text, no struct-layout risk), then enriches each with real SSID,
/// protocol, BSSID, bitrate, and frequency via the Linux Wireless Extensions
/// ioctl API (SIOCGIWESSID etc.) - the same fallback path fastfetch itself
/// uses when the richer nl80211 netlink protocol is unavailable. Netlink
/// (raw AF_NETLINK sockets, generic-netlink family resolution, BSS scan dump
/// parsing, RSN/WPA information-element parsing - see fastfetch's
/// wifi_linux.c) is a substantially larger, unverified-without-Linux-hardware
/// undertaking and deliberately not implemented here.
///
/// Ioctl request codes, struct layouts, and constants below are from the
/// kernel's stable, decades-old &lt;linux/wireless.h&gt; ABI (SIOCGIWSTATS/
/// SIOCGIWENCODEEXT and their struct iw_statistics/iw_encode_ext layouts are
/// intentionally not used here - their field padding could not be confirmed
/// with the same confidence, so signal quality is left to /proc/net/wireless
/// and security is left unset rather than guessed).
/// </summary>
[SupportedOSPlatform("linux")]
public sealed partial class LinuxWifiProbe : IWifiProbe
{
    private const int IfNameSize = 16;
    private const int IwEssidMaxSize = 32;
    private const uint SiocgiwName = 0x8B01;
    private const uint SiocgiwFreq = 0x8B05;
    private const uint SiocgiwAp = 0x8B15;
    private const uint SiocgiwEssid = 0x8B1B;
    private const uint SiocgiwRate = 0x8B21;

    private const int AfInet = 2;
    private const int SockDgram = 2;

    [LibraryImport("libc", SetLastError = true)]
    private static partial int socket(int domain, int type, int protocol);

    [LibraryImport("libc", SetLastError = true)]
    private static partial int close(int fd);

    [LibraryImport("libc", SetLastError = true, EntryPoint = "ioctl")]
    private static partial int ioctl(int fd, uint request, IntPtr argp);

    public IReadOnlyList<WifiConnectionInfo> DetectWifi()
    {
        var results = new List<WifiConnectionInfo>();

        if (!File.Exists("/proc/net/wireless"))
            return results;

        int sock = -1;
        try
        {
            string[] lines = File.ReadAllLines("/proc/net/wireless");
            // Skip the first 2 header lines
            foreach (string line in lines.Skip(2))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                int colonIdx = line.IndexOf(':');
                if (colonIdx <= 0) continue;

                string ifName = line[..colonIdx].Trim();
                string[] parts = line[(colonIdx + 1)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);

                // Quality is typically parts[1] (e.g. "65." or "70")
                int quality = 0;
                if (parts.Length > 1)
                {
                    string qualStr = parts[1].TrimEnd('.');
                    _ = int.TryParse(qualStr, out quality);
                }

                // If sysfs has operstate up
                string operStatePath = $"/sys/class/net/{ifName}/operstate";
                bool isUp = File.Exists(operStatePath) && File.ReadAllText(operStatePath).Trim() == "up";

                if (!isUp && quality <= 0)
                {
                    continue;
                }

                sock = EnsureSocket(sock);

                // SIOCGIWRATE reports one negotiated link rate, not separate Rx/Tx
                // values - set both, since WifiModule only ever displays RxRateMbps.
                double? bitrate = sock >= 0 ? GetBitrateMbps(sock, ifName) : null;

                var connection = new WifiConnectionInfo
                {
                    InterfaceName = ifName,
                    Ssid = sock >= 0 ? GetEssid(sock, ifName) ?? ifName : ifName,
                    SignalQualityPercent = Math.Clamp(quality, 0, 100),
                    Protocol = sock >= 0 ? GetProtocolName(sock, ifName) : null,
                    Bssid = sock >= 0 ? GetBssid(sock, ifName) : null,
                    RxRateMbps = bitrate,
                    TxRateMbps = bitrate,
                    FrequencyBand = sock >= 0 ? GetFrequencyBand(sock, ifName) : null
                };

                results.Add(connection);
            }
        }
        catch
        {
            // Ignore
        }
        finally
        {
            if (sock >= 0)
            {
                close(sock);
            }
        }

        return results;
    }

    private static int EnsureSocket(int existing)
    {
        if (existing >= 0)
        {
            return existing;
        }

        try
        {
            return socket(AfInet, SockDgram, 0);
        }
        catch
        {
            return -1;
        }
    }

    /// <summary>
    /// Allocates a struct iwreq (32 bytes on 64-bit Linux: 16-byte ifrn_name
    /// followed by the iwreq_data union, itself starting at offset 16), copies
    /// the interface name into the first 16 bytes, hands it to the caller to
    /// populate the union portion, issues the ioctl, and always frees the
    /// buffer - callers never see or manage the raw pointer directly.
    /// </summary>
    private static bool TryIoctl(int sock, string ifName, uint request, int bufferSize, Action<IntPtr> populate, out IntPtr buffer)
    {
        buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            for (int i = 0; i < bufferSize; i++)
            {
                Marshal.WriteByte(buffer, i, 0);
            }

            byte[] nameBytes = Encoding.ASCII.GetBytes(ifName);
            int nameLen = Math.Min(nameBytes.Length, IfNameSize - 1);
            Marshal.Copy(nameBytes, 0, buffer, nameLen);

            populate(buffer);

            return ioctl(sock, request, buffer) >= 0;
        }
        catch
        {
            return false;
        }
    }

    private static string? GetEssid(int sock, string ifName)
    {
        IntPtr ssidBuffer = Marshal.AllocHGlobal(IwEssidMaxSize + 1);
        IntPtr iwreqBuffer = IntPtr.Zero;
        try
        {
            for (int i = 0; i <= IwEssidMaxSize; i++)
            {
                Marshal.WriteByte(ssidBuffer, i, 0);
            }

            bool ok = TryIoctl(sock, ifName, SiocgiwEssid, IfNameSize + 16, buf =>
            {
                // struct iw_point at offset 16: void* pointer(8), u16 length(2), u16 flags(2)
                Marshal.WriteIntPtr(buf, IfNameSize, ssidBuffer);
                Marshal.WriteInt16(buf, IfNameSize + 8, (short)(IwEssidMaxSize + 1));
                Marshal.WriteInt16(buf, IfNameSize + 10, 0);
            }, out iwreqBuffer);

            if (!ok)
            {
                return null;
            }

            ushort actualLength = (ushort)Marshal.ReadInt16(iwreqBuffer, IfNameSize + 8);
            if (actualLength == 0 || actualLength > IwEssidMaxSize)
            {
                return null;
            }

            byte[] ssidBytes = new byte[actualLength];
            Marshal.Copy(ssidBuffer, ssidBytes, 0, actualLength);
            string ssid = Encoding.UTF8.GetString(ssidBytes).TrimEnd('\0');
            return ssid.Length > 0 ? ssid : null;
        }
        catch
        {
            return null;
        }
        finally
        {
            Marshal.FreeHGlobal(ssidBuffer);
            if (iwreqBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(iwreqBuffer);
            }
        }
    }

    private static string? GetProtocolName(int sock, string ifName)
    {
        bool ok = TryIoctl(sock, ifName, SiocgiwName, IfNameSize + IfNameSize, _ => { }, out IntPtr buffer);
        try
        {
            if (!ok)
            {
                return null;
            }

            // iwr.u.name is char[IFNAMSIZ] at offset 16, e.g. "IEEE 802.11" - not
            // necessarily NUL-terminated within the fixed buffer, so bound the read.
            byte[] nameBytes = new byte[IfNameSize];
            Marshal.Copy(buffer + IfNameSize, nameBytes, 0, IfNameSize);
            int nul = Array.IndexOf(nameBytes, (byte)0);
            string name = Encoding.ASCII.GetString(nameBytes, 0, nul >= 0 ? nul : IfNameSize).Trim();
            return name.Length > 0 ? name : null;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }

    private static string? GetBssid(int sock, string ifName)
    {
        bool ok = TryIoctl(sock, ifName, SiocgiwAp, IfNameSize + 16, _ => { }, out IntPtr buffer);
        try
        {
            if (!ok)
            {
                return null;
            }

            // iwr.u.ap_addr is struct sockaddr { u16 sa_family; char sa_data[14]; } at
            // offset 16; the MAC lives in the first 6 bytes of sa_data (offset 16+2).
            byte[] mac = new byte[6];
            Marshal.Copy(buffer + IfNameSize + 2, mac, 0, 6);
            if (mac.All(b => b == 0))
            {
                return null;
            }

            return string.Join(":", mac.Select(b => b.ToString("X2")));
        }
        catch
        {
            return null;
        }
        finally
        {
            if (buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }

    private static double? GetBitrateMbps(int sock, string ifName)
    {
        // Always allocate the full 32-byte struct iwreq (16-byte name + 16-byte
        // union), not just the 8 bytes iw_param itself needs - the kernel's ioctl
        // handler operates on the full struct regardless of which union member a
        // given request uses, so a smaller buffer risks an out-of-bounds write.
        bool ok = TryIoctl(sock, ifName, SiocgiwRate, IfNameSize + 16, _ => { }, out IntPtr buffer);
        try
        {
            if (!ok)
            {
                return null;
            }

            // iwr.u.bitrate is struct iw_param { s32 value; ... } at offset 16, in bps.
            int bps = Marshal.ReadInt32(buffer, IfNameSize);
            return bps > 0 ? bps / 1_000_000.0 : null;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }

    private static string? GetFrequencyBand(int sock, string ifName)
    {
        // Same full-struct sizing rationale as GetBitrateMbps above.
        bool ok = TryIoctl(sock, ifName, SiocgiwFreq, IfNameSize + 16, _ => { }, out IntPtr buffer);
        try
        {
            if (!ok)
            {
                return null;
            }

            // iwr.u.freq is struct iw_freq { s32 m; s16 e; u8 i; u8 flags; } at offset 16.
            int m = Marshal.ReadInt32(buffer, IfNameSize);
            short e = Marshal.ReadInt16(buffer, IfNameSize + 4);
            if (m <= 0)
            {
                return null;
            }

            double hz = m * Math.Pow(10, e);
            double ghz = hz / 1_000_000_000.0;
            return ghz switch
            {
                >= 5.9 and < 7.5 => "6 GHz",
                >= 4.9 and < 5.9 => "5 GHz",
                >= 2.3 and < 2.6 => "2.4 GHz",
                _ => null
            };
        }
        catch
        {
            return null;
        }
        finally
        {
            if (buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }
}
