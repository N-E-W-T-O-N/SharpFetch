# System Specifications Detection: Comprehensive Cross-Repository Analysis & Observations

## 1. Executive Summary

This document provides a deep architectural and low-level technical analysis of how modern system fetch utilities and hardware libraries inspect operating system and hardware specifications across different OS platforms (Windows, Linux, macOS, BSD) and CPU architectures (x86_64, ARM64, etc.).

We analyzed four primary reference implementations present in this workspace:
1. **`fastfetch`** (C / C++): The industry standard for high-performance, sub-millisecond hardware detection using direct operating system kernel interfaces, NT system calls, Win32 APIs, SetupAPI/D3DKMT, procfs/sysfs, and binary SMBIOS parsing.
2. **`neofetch`** (Bash): The classic CLI fetch script relying on POSIX pipeline scripting, virtual filesystem reading, sysctl, and external subprocess execution (`lspci`, `lscpu`, `system_profiler`, `wmic`, `xrandr`).
3. **`winfetch`** (PowerShell 5+/7+): A Windows-specific fetch script combining PowerShell CIM/WMI cmdlets, direct Windows Registry queries, and .NET Base Class Library (`[System.Environment]`, `[System.Windows.Forms.SystemInformation]`, `[System.IO.DriveInfo]`).
4. **`Hardware.Info`** (.NET / C#): A cross-platform .NET hardware library dividing platform logic into `Hardware.Info.Windows` (WMI + P/Invoke), `Hardware.Info.Linux` (procfs + sysfs + CLI fallbacks), and `Hardware.Info.Mac` (sysctl + `system_profiler` + PList).

---

## 2. Core OS Mechanisms: How Data is Exposed

Operating systems expose hardware, topology, and telemetry through distinct layers:

```
+---------------------------------------------------------------------------------------------------+
|                                 APPLICATION / CLI LAYER                                           |
|                     (SharpFetch, fastfetch, neofetch, winfetch, Hardware.Info)                   |
+---------------------------------+---------------------------------+-------------------------------+
|             LINUX               |             WINDOWS             |             macOS             |
+---------------------------------+---------------------------------+-------------------------------+
| • Virtual Filesystems           | • Direct Win32 / NT APIs        | • sysctl / sysctlbyname       |
|   - /proc/cpuinfo, /proc/meminfo|   - GlobalMemoryStatusEx        |   - hw.memsize, hw.ncpu       |
|   - /proc/stat, /proc/uptime    |   - GetSystemFirmwareTable      |   - machdep.cpu.brand_string  |
|   - /sys/class/dmi/id/*         |   - NtQuerySystemInformation    | • IOKit Framework             |
|   - /sys/devices/system/cpu/*   |   - D3DKMTEnumAdapters2 (GPU)   |   - IOServiceMatching         |
|   - /sys/class/drm/* (EDID)     |   - DeviceIoControl (Storage)   |   - IOPMPowerSource           |
|   - /sys/class/power_supply/*   | • Windows Registry              | • CoreFoundation APIs         |
| • POSIX / C Syscalls            |   - HKLM\HARDWARE\DESCRIPTION   | • Subprocess Profiling        |
|   - uname(), sysinfo()          |   - HKLM\SOFTWARE\Microsoft\NT  |   - sw_vers, pmset -g batt    |
|   - clock_gettime(CLOCK_BOOT)   | • WMI / CIM (root\cimv2)        |   - system_profiler           |
| • Subprocess Fallbacks          |   - Win32_Processor, etc.       |   - vm_stat, ioreg            |
|   - lspci, lshw, xrandr         | • KUSER_SHARED_DATA (0x7FFE0000)|                               |
+---------------------------------+---------------------------------+-------------------------------+
```

---

## 3. Subsystem-by-Subsystem Technical Analysis

### 3.1 Operating System, Kernel, and Architecture

| Platform | `fastfetch` (C) | `neofetch` (Bash) | `winfetch` (PowerShell) | `Hardware.Info` (C#) |
| :--- | :--- | :--- | :--- | :--- |
| **Windows** | `BrandingFormatString(L"%WINDOWS_LONG%")` + Registry fallback (`HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion` -> `ProductName`, `DisplayVersion`, `CurrentBuild`) | `wmic os get Caption` | `Get-CimInstance Win32_OperatingSystem` + `$PSVersionTable` | `RtlGetVersion` (ntdll) + WMI `Win32_OperatingSystem` fallback |
| **Linux** | Parses `/etc/os-release` (`NAME`, `PRETTY_NAME`, `VERSION_ID`), `uname()` for kernel and architecture | Parses `/etc/os-release`, `uname -r`, `uname -m` | N/A (Windows only) | Parses `/etc/os-release` (`NAME=`, `VERSION_ID=`) |
| **macOS** | `sysctlbyname("kern.osproductversion")`, `sw_vers`, `uname()` | `sw_vers -productVersion`, `sw_vers -buildVersion`, `uname -r` | N/A | Subprocess `sw_vers -productName` & `sw_vers -productVersion` |

> **Key Observation**: On Windows, calling WMI `Win32_OperatingSystem` takes 40–120 ms. In contrast, reading `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion` directly in C# via `Microsoft.Win32.Registry` takes **< 0.2 ms** and reliably returns `ProductName`, `DisplayVersion` (e.g. `23H2`), and `CurrentBuildNumber`. On Linux, parsing `/etc/os-release` via `File.ReadAllLines` is instantaneous.

---

### 3.2 CPU (Model, Cores, Threads, Frequency, Architecture, Caches)

| Metric | Linux Mechanism | Windows Mechanism | macOS Mechanism |
| :--- | :--- | :--- | :--- |
| **Model Name** | `/proc/cpuinfo` (`model name` or `Hardware`) | Registry: `HKLM\HARDWARE\DESCRIPTION\System\CentralProcessor\0\ProcessorNameString` | `sysctlbyname("machdep.cpu.brand_string")` |
| **Base Clock** | `/sys/devices/system/cpu/cpu0/cpufreq/scaling_max_freq` or `/proc/cpuinfo` (`cpu MHz`) | Registry: `HKLM\HARDWARE\DESCRIPTION\System\CentralProcessor\0\~MHz` | `sysctlbyname("hw.cpufrequency")` |
| **Physical & Logical Cores** | `/proc/cpuinfo` (`physical id`, `core id`, `cpu cores`, `siblings`) | `GetLogicalProcessorInformationEx` (supports P/E hybrid clusters & NUMA) | `sysctlbyname("hw.physicalcpu")` & `hw.logicalcpu` |
| **Caches (L1/L2/L3)** | `/sys/devices/system/cpu/cpu*/cache/index*/` (`size`, `level`, `type`) | SMBIOS Type 7 (Cache Info) or `GetLogicalProcessorInformationEx(RelationCache)` | `sysctlbyname("hw.l1dcachesize")`, `hw.l2cachesize`, `hw.l3cachesize` |
| **P-Core / E-Core Detection** | `/sys/devices/system/cpu/cpu*/topology/` or `/sys/devices/system/cpu/cpu*/cpufreq/cpuinfo_max_freq` | `GetLogicalProcessorInformationEx` (`RelationProcessorCore` -> `EfficiencyClass`) | `sysctl -n hw.nperflevels` -> `hw.perflevel0.*` (P), `hw.perflevel1.*` (E) |
| **CPU Usage %** | Delta between two reads of `/proc/stat` (`user + nice + system + idle + iowait + irq`) | `GetSystemTimes` (kernel32) or `NtQuerySystemInformation(SystemPerformanceInformation)` | `host_statistics64(mach_host_self(), HOST_CPU_LOAD_INFO, ...)` |
| **CPU Temperature** | `/sys/class/hwmon/hwmon*/temp*_input` or `/sys/class/thermal/thermal_zone*/temp` | Windows Thermal Zone Perflib (`\ _TZ.CPUZ`) or WMI `MSAcpi_ThermalZoneTemperature` (Admin) | SMC read via IOKit (`smc_temps.c`) |

> **Key Observation**:
> - `neofetch` and `winfetch` parse the registry or `wmic` for basic name/frequency.
> - `fastfetch` uses `GetLogicalProcessorInformationEx` on Windows which gives complete physical cores, logical threads, NUMA nodes, cache topology, and **Intel 12th+ Gen Hybrid P/E core distribution** via `EfficiencyClass`.
> - In .NET, `System.Environment.ProcessorCount` gives logical thread count instantly. To get CPU name & clock on Windows without WMI, reading the Registry key `CentralProcessor\0` is the gold standard.

---

### 3.3 GPU & Graphics Controller

| Platform | Native Mechanism | Subprocess / WMI Alternative | Details & Capabilities |
| :--- | :--- | :--- | :--- |
| **Windows** | `D3DKMTEnumAdapters2` & `D3DKMTQueryAdapterInfo` (`GDI32.dll` / Direct3D Kernel Mode Thunk) | WMI `Win32_VideoController` | `D3DKMT` queries all display adapters, VRAM local/non-local capacity & live usage, WDDM version, PCIe link speed/width, without initializing DirectX device contexts. |
| **Linux** | `lspci` / `/sys/bus/pci/devices/` matching PCI classes `0x0300` (VGA), `0x0302` (3D), `0x0380` (Display) | `glxinfo`, `nvidia-smi` | Reads PCI vendor/device IDs from `/sys/bus/pci/devices/*/uevent` or `lspci`. For WSL2: `/dev/dxg` provides direct Windows D3DKMT bridge. |
| **macOS** | IOKit registry (`IOAccelerator` / `IOPCIDevice`) | `system_profiler SPDisplaysDataType` | Retrieves chipset model, VRAM size, vendor, Metal support version. |

> **Key Observation**:
> - In Windows, WMI `Win32_VideoController` is simple (`Name`, `AdapterRAM`, `DriverVersion`) but slow (~80ms).
> - In Windows Registry, `HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000` (and `0001`, `0002`...) contains `DriverDesc`, `ProviderName`, `DriverVersion`, `HardwareInformation.qwMemorySize` without any WMI overhead.
> - On Linux, inspecting `/sys/class/drm/card*/device/` or parsing `lspci` allows GPU enumeration without loading heavy OpenGL/Vulkan libraries.

---

### 3.4 Memory (RAM) & Swap

| Platform | Total / Free Memory | Used Memory Calculation |
| :--- | :--- | :--- |
| **Windows** | `GlobalMemoryStatusEx` (`kernel32.dll`) | `TotalPhys = ullTotalPhys`, `UsedPhys = ullTotalPhys - ullAvailPhys`. Fast, exact, 0 allocations. |
| **Linux** | `/proc/meminfo` | `MemTotal` - (`MemAvailable` or `MemFree + Buffers + Cached + SReclaimable - Shmem`). ZFS ARC cache adjustments can be subtracted from `/proc/spl/kstat/zfs/arcstats`. |
| **macOS** | `sysctlbyname("hw.memsize")` + `vm_stat` / Mach VM statistics | `(pageable_internal + wired_count + compressed) * pagesize` |
| **BSD** | `sysctl hw.physmem` / `hw.physmem64` | `Total - (vm.stats.vm.v_free_count * pagesize)` |

> **Key Observation**:
> - Windows `GlobalMemoryStatusEx` via P/Invoke is a 4-line C# struct call that runs in **< 0.01 ms**.
> - In .NET on Linux, reading `/proc/meminfo` with `File.OpenRead` and parsing the first 10 lines is virtually instantaneous.

---

### 3.5 Disks & Physical Storage

| Aspect | Windows | Linux | macOS |
| :--- | :--- | :--- | :--- |
| **Mounted Drives / Volumes** | `[System.IO.DriveInfo]::GetDrives()` (Type, TotalSize, AvailableFreeSpace, VolumeLabel, DriveFormat) | `statvfs()` syscall or parsing `/proc/mounts` + `df -k` | `getmntinfo()` syscall or `statfs()` |
| **Physical Disk Hardware** | `DeviceIoControl` (`IOCTL_STORAGE_QUERY_PROPERTY`) on `\\.\PhysicalDriveN` -> `STORAGE_DEVICE_DESCRIPTOR` (BusType, NVMe/SATA/USB, Rotational SSD vs HDD) | `/sys/block/*/queue/rotational` (`0` = SSD/NVMe, `1` = HDD) + `/sys/block/*/size` | IOKit `IOBlockStorageDevice` or `system_profiler SPStorageDataType` |

> **Key Observation**:
> .NET's built-in `System.IO.DriveInfo.GetDrives()` is cross-platform across Windows, Linux, and macOS, providing total, available, and used filesystem space with zero external dependencies.

---

### 3.6 Motherboard, Host Computer Model & BIOS / Firmware

| Component | Windows | Linux | macOS |
| :--- | :--- | :--- | :--- |
| **SMBIOS Table Access** | `GetSystemFirmwareTable('RSMB', 0, ...)` (kernel32) | `/sys/firmware/dmi/tables/DMI` or `/sys/class/dmi/id/*` | `IOServiceMatching("AppleSMBIOS")` |
| **Board Manufacturer / Model** | SMBIOS Type 2 (Baseboard) or Registry `HKLM\HARDWARE\DESCRIPTION\System\BIOS` (`BaseBoardManufacturer`, `BaseBoardProduct`) | `/sys/class/dmi/id/board_vendor`, `/sys/class/dmi/id/board_name` | IOKit `hw.model` |
| **Host System Model** | SMBIOS Type 1 (System Info: `Manufacturer`, `ProductName`, `UUID`, `SKUNumber`, `Family`) | `/sys/class/dmi/id/product_name`, `/sys/class/dmi/id/sys_vendor` | `sysctl -n hw.model` |
| **BIOS Vendor & Version** | Registry `HKLM\HARDWARE\DESCRIPTION\System\BIOS` (`BIOSVendor`, `BIOSVersion`, `BIOSReleaseDate`) | `/sys/class/dmi/id/bios_vendor`, `/sys/class/dmi/id/bios_version`, `/sys/class/dmi/id/bios_date` | `system_profiler SPHardwareDataType` (`System Firmware Version`) |

> **Key Observation**:
> - In Windows, both `winfetch` and `Hardware.Info` use WMI (`Win32_BaseBoard`, `Win32_ComputerSystem`, `Win32_BIOS`).
> - However, the Windows Registry key `HKLM\HARDWARE\DESCRIPTION\System\BIOS` contains all of these fields pre-populated at boot time! Reading this key avoids WMI overhead completely.

---

### 3.7 Battery & Power Management

| Platform | Preferred Native Method | Alternative Method |
| :--- | :--- | :--- |
| **Windows** | `GetSystemPowerStatus` (`kernel32.dll`) | `[System.Windows.Forms.SystemInformation]::PowerStatus` or WMI `Win32_Battery` |
| **Linux** | `/sys/class/power_supply/BAT*/` (`capacity`, `status`, `energy_now`, `energy_full`, `power_now`, `charge_now`) | UPower D-Bus interface (`org.freedesktop.UPower`) |
| **macOS** | `IOPMPowerSource` via IOKit | `pmset -g batt` subprocess |

> **Key Observation**:
> On Windows, `GetSystemPowerStatus` in `kernel32.dll` takes a simple struct `SYSTEM_POWER_STATUS` (ACLineStatus, BatteryFlag, BatteryLifePercent, BatteryLifeTime, BatteryFullLifeTime) and executes instantly.

---

### 3.8 Monitors, Display Resolution & Refresh Rate

| Platform | Detection Strategy |
| :--- | :--- |
| **Windows** | 1. `QueryDisplayConfig` (`user32.dll`): Retrieves active display paths, EDID names, modes, pixel resolution, refresh rate numerator/denominator.<br>2. `EnumDisplayMonitors` / `GetMonitorInfoW` / `EnumDisplaySettingsW` (`user32.dll`).<br>3. `[System.Windows.Forms.Screen]::AllScreens` (.NET BCL). |
| **Linux** | 1. `/sys/class/drm/card*-*/status` (look for `connected`) + parse 128-byte binary `/sys/class/drm/card*-*/edid`.<br>2. `xrandr --props` or Wayland compositor protocol (`wlr-output-management`). |
| **macOS** | `CGGetActiveDisplayList` / `CGDisplayModeGetRefreshRate` via CoreGraphics, or `system_profiler SPDisplaysDataType`. |

---

### 3.9 System Uptime & Boot Time

| Platform | Method | Accuracy & Performance |
| :--- | :--- | :--- |
| **Windows** | `GetTickCount64()` (kernel32) or `KUSER_SHARED_DATA->InterruptTime` (at `0x7FFE0008`) | Sub-microsecond execution, millisecond resolution. `Environment.TickCount64` in .NET uses `GetTickCount64` natively. |
| **Linux** | `/proc/uptime` (first float) or `clock_gettime(CLOCK_BOOTTIME)` | Exact seconds since boot. |
| **macOS** | `sysctlbyname("kern.boottime")` | Returns `struct timeval` of boot timestamp. |

> **Key Observation**:
> In modern .NET (including .NET 9), `TimeSpan.FromMilliseconds(Environment.TickCount64)` provides system uptime out of the box across Windows and Linux.

---

### 3.10 Terminal & Shell Detection

| Target | Detection Technique |
| :--- | :--- |
| **Shell** | 1. Windows: Traverse parent process ID (PPID) using `CreateToolhelp32Snapshot` / `Process.GetProcessById(ppid)`. Match executable name (`pwsh.exe`, `powershell.exe`, `cmd.exe`, `bash.exe`, `nu.exe`, `zsh.exe`).<br>2. Linux/macOS: `$SHELL` env variable or PPID process name via `/proc/$PPID/cmdline`. |
| **Terminal** | Inspect environment variables in order: `WT_SESSION` (Windows Terminal), `ALACRITTY_LOG` / `ALACRITTY_WINDOW_ID` (Alacritty), `VSCODE_INJECTION` / `TERM_PROGRAM` (VS Code / iTerm2 / WezTerm), `ConEmuPID` (ConEmu), `SSH_TTY` / `SSH_CONNECTION` (SSH Session), `TERM` (fallback), then parent process name (e.g. `conhost.exe`, `WindowsTerminal.exe`, `gnome-terminal`, `kitty`). |

---

### 3.11 Package Managers

| Package Manager | Windows Discovery | Linux / macOS Discovery |
| :--- | :--- | :--- |
| **Winget** | `winget list --disable-interactivity` | N/A |
| **Scoop** | Count directories in `%USERPROFILE%\scoop\apps` and `%ProgramData%\scoop\apps` (minus `scoop` itself) | N/A |
| **Chocolatey** | Count directories in `%ChocolateyInstall%\lib` (or `$env:ProgramData\chocolatey\lib`) | N/A |
| **Pacman (MSYS2 / Arch)** | Count folders in `/var/lib/pacman/local` | Count folders in `/var/lib/pacman/local` |
| **DPKG / APT** | N/A | Count `^Status: install ok installed` in `/var/lib/dpkg/status` |
| **RPM** | N/A | Query SQLite DB `/var/lib/rpm/rpmdb.sqlite` or `/var/lib/rpm/Packages` |
| **Homebrew** | N/A | Count folders in `/opt/homebrew/Cellar` or `/usr/local/Cellar` |
| **Flatpak / Snap** | N/A | Count folders in `/var/lib/flatpak/app` / `/var/lib/snapd/snaps` |

> **Key Observation**:
> `fastfetch` counts packages in Scoop and Chocolatey by **reading directory counts on the filesystem** rather than spawning `scoop list` or `choco list` processes. This transforms a 1500 ms CLI invocation into a **0.5 ms directory scan**.

---

## 4. Repository Comparison Matrix

| Feature / Criteria | `fastfetch` | `neofetch` | `winfetch` | `Hardware.Info` | Target: `SharpFetch` |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Language / Runtime** | C / C++ | Bash (3.2+) | PowerShell (5.1/7+) | C# / .NET 8-9 | C# / .NET 9 (AOT ready) |
| **Execution Time** | ~1 - 5 ms | ~200 - 2500 ms | ~100 - 400 ms | ~50 - 500 ms | **< 15 ms target** |
| **Primary Windows Source** | Registry + P/Invoke + D3DKMT + SMBIOS | `wmic` via MSYS2/Cygwin | WMI/CIM + Registry + .NET | WMI + P/Invoke | **Registry + Win32 P/Invoke + BCL** |
| **Primary Linux Source** | `/proc`, `/sys`, `uname`, `ioctl` | `/proc`, `/sys`, CLI tools | N/A | `/proc`, `/sys`, `lshw`, `lspci` | **`/proc`, `/sys`, `uname`, BCL** |
| **Primary macOS Source** | `sysctl`, IOKit, CoreFoundation | `sysctl`, `system_profiler`, `vm_stat` | N/A | `sysctl`, `system_profiler` | **`sysctl` P/Invoke + BCL** |
| **Terminal UI Output** | ANSI strings + Logos | ANSI strings + Logos | PowerShell ANSI formatting | Plain data objects | **Spectre.Console (Rich ASCII, Widgets, Panels)** |
| **Native AOT Compatible** | Native Binary | Interpreted | Interpreted | Partial (Aot subproject) | **100% Native AOT Compatible** |

---

## 5. Architectural Blueprint for `SharpFetch` (.NET 9)

To build the fastest, cleanest, and most reliable .NET CLI system fetcher, `SharpFetch` should adopt the following patterns:

### 5.1 Layered Structure

```
SharpFetch/
├── Program.cs                  # Entrypoint, CLI options parsing, Spectre.Console renderer
├── Core/
│   ├── Models/                 # Immutable DTO records (SystemSpecs, CpuInfo, GpuInfo, MemoryInfo, etc.)
│   ├── ISystemProbe.cs         # Interface for platform-specific hardware probing
│   └── SystemInfoProvider.cs   # Orchestrator & fallback aggregator
├── Platforms/
│   ├── Windows/
│   │   ├── WindowsProbe.cs     # Registry + Win32 P/Invoke implementation
│   │   ├── NativeMethods.cs    # GlobalMemoryStatusEx, GetSystemPowerStatus, etc.
│   │   └── WindowsRegistry.cs  # Direct fast registry readers
│   ├── Linux/
│   │   ├── LinuxProbe.cs       # /proc/cpuinfo, /proc/meminfo, /sys/class/dmi reader
│   │   └── ProcFsReader.cs     # Zero-allocation span-based file parser
│   └── MacOS/
│       ├── MacProbe.cs         # sysctlbyname P/Invoke implementation
│       └── Sysctl.cs           # P/Invoke wrapper for libc sysctl
└── UI/
    ├── AsciiLogos.cs           # Vectorized / colored ASCII logos for OSes
    ├── ColorPalettes.cs        # Accent colors matching OS branding
    └── ConsoleRenderer.cs      # Spectre.Console layout, panels, tables, and progress bars
```

### 5.2 Key Performance Principles for C# / .NET

1. **Avoid WMI on Windows**:
   - Instead of `ManagementObjectSearcher` or `Get-CimInstance`, read `HKLM\HARDWARE\DESCRIPTION\System\CentralProcessor\0` and `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion`.
   - Use `GlobalMemoryStatusEx` for RAM and `GetSystemPowerStatus` for battery via `[LibraryImport]` / `[DllImport]`.
2. **Span-based Parsing on Linux**:
   - Use `File.OpenRead` with `stackalloc byte[]` or `StreamReader` to parse `/proc/meminfo` and `/proc/cpuinfo` without generating garbage collector pressure.
3. **Directory Counting for Package Managers**:
   - Use `Directory.EnumerateDirectories()` on `%USERPROFILE%\scoop\apps` and `ChocolateyInstall\lib` rather than invoking `winget.exe`, `scoop.cmd`, or `choco.exe` unless explicitly requested.
4. **Leverage Native .NET BCL Built-ins**:
   - `Environment.TickCount64` for Uptime.
   - `Environment.MachineName`, `Environment.UserName`, `Environment.OSVersion`.
   - `RuntimeInformation.OSArchitecture`, `RuntimeInformation.ProcessArchitecture`, `RuntimeInformation.FrameworkDescription`.
   - `DriveInfo.GetDrives()` for storage volumes.
5. **Native AOT Ready**:
   - Avoid COM interop and dynamic reflection to allow compiling `SharpFetch` into a standalone, instant-startup native binary (`dotnet publish -c Release -r win-x64 /p:PublishAot=true`).
