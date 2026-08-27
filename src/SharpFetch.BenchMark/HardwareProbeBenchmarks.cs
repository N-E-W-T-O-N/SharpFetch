using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Win32;

namespace SharpFetch.Benchmark;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0, baseline: true)]
[SimpleJob(RuntimeMoniker.NativeAot10_0)]
[SupportedOSPlatform("windows")]
public class HardwareProbeBenchmarks
{
    // ==========================================
    // 1. RAM / Memory Benchmark (GlobalMemoryStatusEx)
    // ==========================================
    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [Benchmark(Description = "RAM: GlobalMemoryStatusEx (Native Win32)")]
    public (ulong total, ulong used, ulong free) BenchmarkMemoryStatus()
    {
        var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (GlobalMemoryStatusEx(ref status))
        {
            ulong used = status.ullTotalPhys - status.ullAvailPhys;
            return (status.ullTotalPhys, used, status.ullAvailPhys);
        }
        return (0, 0, 0);
    }

    // ==========================================
    // 2. CPU Specs Benchmark (Windows Registry)
    // ==========================================
    [Benchmark(Description = "CPU: Registry CentralProcessor/0")]
    public (string name, int mhz) BenchmarkCpuSpecs()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
        if (key != null)
        {
            string name = (key.GetValue("ProcessorNameString") as string)?.Trim() ?? "Unknown Processor";
            int mhz = (int)(key.GetValue("~MHz") ?? 0);
            return (name, mhz);
        }
        return ("Unknown", 0);
    }

    // ==========================================
    // 3. OS Specs Benchmark (Windows Registry)
    // ==========================================
    [Benchmark(Description = "OS: Registry CurrentVersion")]
    public (string product, string displayVer, string build) BenchmarkOsSpecs()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        if (key != null)
        {
            string product = key.GetValue("ProductName") as string ?? "Windows";
            string displayVer = key.GetValue("DisplayVersion") as string ?? "";
            string build = key.GetValue("CurrentBuildNumber") as string ?? "";
            return (product, displayVer, build);
        }
        return ("Windows", "", "");
    }

    // ==========================================
    // 4. Motherboard & BIOS Benchmark (Registry)
    // ==========================================
    [Benchmark(Description = "Motherboard: Registry System/BIOS")]
    public (string vendor, string product, string biosVer) BenchmarkBiosSpecs()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS");
        if (key != null)
        {
            string vendor = key.GetValue("BaseBoardManufacturer") as string ?? key.GetValue("SystemManufacturer") as string ?? "";
            string product = key.GetValue("BaseBoardProduct") as string ?? key.GetValue("SystemProductName") as string ?? "";
            string biosVer = key.GetValue("BIOSVersion") as string ?? "";
            return (vendor, product, biosVer);
        }
        return ("", "", "");
    }

    // ==========================================
    // 5. Battery Status Benchmark (GetSystemPowerStatus)
    // ==========================================
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

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);

    [Benchmark(Description = "Battery: GetSystemPowerStatus (Native Win32)")]
    public (bool hasBattery, byte percent, bool isCharging) BenchmarkBatteryStatus()
    {
        if (GetSystemPowerStatus(out var status) && status.BatteryFlag != 128 && status.BatteryLifePercent != 255)
        {
            bool isCharging = (status.BatteryFlag & 8) != 0;
            return (true, status.BatteryLifePercent, isCharging);
        }
        return (false, 0, false);
    }

    // ==========================================
    // 6. Uptime Benchmark (Environment.TickCount64)
    // ==========================================
    [Benchmark(Description = "Uptime: Environment.TickCount64")]
    public TimeSpan BenchmarkUptime()
    {
        return TimeSpan.FromMilliseconds(Environment.TickCount64);
    }

    // ==========================================
    // 7. Storage Volumes Benchmark (DriveInfo.GetDrives)
    // ==========================================
    [Benchmark(Description = "Storage: DriveInfo.GetDrives()")]
    public int BenchmarkDriveList()
    {
        int readyCount = 0;
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.IsReady)
            {
                readyCount++;
            }
        }
        return readyCount;
    }
}
