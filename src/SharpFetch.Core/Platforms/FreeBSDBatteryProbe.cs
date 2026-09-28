using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms;

[SupportedOSPlatform("freebsd")]
public sealed partial class FreeBSDBatteryProbe : IBatteryProbe
{
    [LibraryImport("libc", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int sysctlbyname(
        string name,
        Span<byte> oldp,
        ref nuint oldlenp,
        IntPtr newp,
        nuint newlen);

    public IReadOnlyList<BatteryInfo> DetectBatteries()
    {
        int life = GetSysctlInt32("hw.acpi.battery.life");
        if (life < 0 || life > 100)
        {
            return [];
        }

        int stateRaw = GetSysctlInt32("hw.acpi.battery.state");
        int timeMinutes = GetSysctlInt32("hw.acpi.battery.time");
        int acline = GetSysctlInt32("hw.acpi.acline");

        bool isAc = acline == 1;

        // ACPI battery states:
        // 0 = none / full
        // 1 = discharging
        // 2 = charging
        // 4 = critical
        BatteryState state;
        if ((stateRaw & 2) != 0)
        {
            state = BatteryState.Charging;
        }
        else if (life == 100 && isAc)
        {
            state = BatteryState.Full;
        }
        else if ((stateRaw & 1) != 0)
        {
            state = BatteryState.Discharging;
        }
        else if (isAc)
        {
            state = BatteryState.NotCharging;
        }
        else
        {
            state = BatteryState.Unknown;
        }

        TimeSpan? timeRemaining = timeMinutes > 0
            ? TimeSpan.FromMinutes(timeMinutes)
            : null;

        var info = new BatteryInfo
        {
            DeviceName = "Internal",
            CapacityPercent = life,
            State = state,
            IsAcConnected = isAc,
            TimeRemaining = timeRemaining
        };

        return [info];
    }

    private static int GetSysctlInt32(string name)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        nuint len = (nuint)buffer.Length;
        return sysctlbyname(name, buffer, ref len, IntPtr.Zero, 0) == 0 && len >= sizeof(int)
            ? MemoryMarshal.Read<int>(buffer)
            : -1;
    }
}
