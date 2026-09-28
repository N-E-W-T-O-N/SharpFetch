using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms;

[SupportedOSPlatform("windows")]
public sealed partial class WindowsBatteryProbe : IBatteryProbe
{
    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);

    public IReadOnlyList<BatteryInfo> DetectBatteries()
    {
        if (!GetSystemPowerStatus(out var status))
        {
            return [];
        }

        // Flag 128 indicates no system battery installed (desktop PC)
        // 255 indicates unknown status / no battery
        if ((status.BatteryFlag & 128) != 0 ||
            (status.BatteryFlag == 255 && status.BatteryLifePercent == 255))
        {
            return [];
        }

        if (status.BatteryLifePercent > 100 && status.BatteryLifePercent != 255)
        {
            return [];
        }

        bool isAc = status.ACLineStatus == 1;
        bool isCharging = (status.BatteryFlag & 8) != 0;

        BatteryState state;
        if (isCharging)
        {
            state = BatteryState.Charging;
        }
        else if (status.BatteryLifePercent == 100 && isAc)
        {
            state = BatteryState.Full;
        }
        else if (isAc)
        {
            state = BatteryState.NotCharging;
        }
        else if (status.ACLineStatus == 0)
        {
            state = BatteryState.Discharging;
        }
        else
        {
            state = BatteryState.Unknown;
        }

        TimeSpan? timeRemaining = status.BatteryLifeTime > 0
            ? TimeSpan.FromSeconds(status.BatteryLifeTime)
            : null;

        var info = new BatteryInfo
        {
            DeviceName = "Internal",
            CapacityPercent = status.BatteryLifePercent != 255 ? status.BatteryLifePercent : 0,
            State = state,
            IsAcConnected = isAc,
            TimeRemaining = timeRemaining
        };

        return [info];
    }
}
