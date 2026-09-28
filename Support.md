# SharpFetch Component & Device Support Checklist

> **Current Implementation Status**: 🟢 **24% Implemented (OS, Host, Kernel, CPU, GPU, Display, Memory, Swap, Disk, Local IP, Wi-Fi, Battery & Uptime Active)**  
> This checklist tracks what hardware and system telemetry components `SharpFetch` can read and display.

### Status Legend
* `[*]` **Implemented & Verified**: Code is implemented, tested, and actively working in `SharpFetch`.
* `[.]` **In Progress / Partial**: Basic stub or partial platform support written; requires refinement or testing.
* `[ ]` **Pending / Not Implemented**: Not yet implemented in the codebase.

---

## Fastfetch 74-Module Feature Checklist

- [*] 01) Battery       : Print battery capacity, status, etc
- [ ] 02) BIOS          : Print information of 1st-stage bootloader (name, version, release date, etc)
- [ ] 03) Bluetooth     : List (connected) bluetooth devices
- [ ] 04) BluetoothRadio: List bluetooth radios width supported version and vendor
- [ ] 05) Board         : Print motherboard name and other info
- [ ] 06) Bootmgr       : Print information of 2nd-stage bootloader (name, firmware, etc)
- [*] 07) Break         : Print a empty line
- [ ] 08) Brightness    : Print current brightness level of your monitors
- [ ] 09) Btrfs         : Print Linux BTRFS volumes
- [ ] 10) Camera        : Print available cameras
- [ ] 11) Chassis       : Print chassis type (desktop, laptop, etc)
- [ ] 12) Command       : Run custom shell scripts
- [*] 13) Colors        : Display the terminal's 16-color palette
- [*] 14) CPU           : Print CPU name, frequency, etc
- [ ] 15) CPUCache      : Print CPU cache sizes
- [ ] 16) CPUUsage      : Print CPU usage. Costs some time to collect data
- [ ] 17) Cursor        : Print cursor style name
- [ ] 18) Custom        : Print a custom string, with or without key
- [ ] 19) DateTime      : Print current date and time
- [ ] 20) DE            : Print desktop environment name
- [*] 21) Display       : Print resolutions, refresh rates, etc
- [*] 22) Disk          : Print partitions, space usage, file system, etc
- [ ] 23) DiskIO        : Print physical disk I/O throughput
- [ ] 24) DNS           : Print configured DNS servers
- [ ] 25) Editor        : Print information of the default editor ($VISUAL or $EDITOR)
- [ ] 26) Font          : Print system font names
- [ ] 27) Gamepad       : List (connected) gamepads
- [*] 28) GPU           : Print GPU names, graphic memory size, type, etc
- [*] 29) Host          : Print product name of your computer
- [ ] 30) Icons         : Print icon style name
- [ ] 31) InitSystem    : Print init system (pid 1) name and version
- [*] 32) Kernel        : Print system kernel version
- [ ] 33) Keyboard      : List (connected) keyboards
- [ ] 34) LM            : Print login manager (desktop manager) name and version
- [ ] 35) Loadavg       : Print system load averages
- [ ] 36) Locale        : Print system locale name
- [*] 37) LocalIp       : List local IP addresses (v4 or v6), MAC addresses, etc
- [.] 38) Logo          : Query built-in logo for JSON output
- [ ] 39) Media         : Print playing song name
- [*] 40) Memory        : Print system memory usage info
- [*] 41) Monitor       : Alias of Display module
- [ ] 42) Mouse         : List connected mouses
- [ ] 43) NetIO         : Print network I/O throughput
- [ ] 44) OpenCL        : Print highest OpenCL version supported by the GPU
- [ ] 45) OpenGL        : Print highest OpenGL version supported by the GPU
- [*] 46) OS            : Print operating system name and version
- [ ] 47) Packages      : List installed package managers and count of installed packages
- [ ] 48) PhysicalDisk  : Print physical disk information
- [ ] 49) PhysicalMemory: Print system physical memory devices
- [ ] 50) Player        : Print music player name
- [ ] 51) PowerAdapter  : Print power adapter name and charging watts
- [ ] 52) Processes     : Print number of running processes
- [ ] 53) PublicIp      : Print your public IP address, etc
- [*] 54) Separator     : Print a separator line
- [ ] 55) Shell         : Print current shell name and version
- [ ] 56) Sound         : Print sound devices, volume, etc
- [*] 57) Swap          : Print swap (paging file) space usage
- [ ] 58) Terminal      : Print current terminal name and version
- [ ] 59) TerminalFont  : Print font name and size used by current terminal
- [ ] 60) TerminalSize  : Print current terminal size
- [ ] 61) TerminalTheme : Print current terminal theme (foreground and background colors)
- [*] 62) Title         : Print title, which contains your user name, hostname
- [ ] 63) Theme         : Print current theme of desktop environment
- [ ] 64) TPM           : Print info of Trusted Platform Module (TPM) Security Device
- [*] 65) Uptime        : Print how long system has been running
- [ ] 66) Users         : Print users currently logged in
- [ ] 67) Version       : Print Fastfetch version
- [ ] 68) Vulkan        : Print highest Vulkan version supported by the GPU
- [ ] 69) Wallpaper     : Print image file path of current wallpaper
- [ ] 70) Weather       : Print weather information
- [ ] 71) WM            : Print window manager name and version
- [*] 72) Wifi          : Print connected Wi-Fi info (SSID, connection and security protocol)
- [ ] 73) WMTheme       : Print current theme of window manager
- [ ] 74) Zpool         : Print ZFS storage pools

---

## 1. Operating System & Kernel Telemetry

| Component / Metric | Current Status in `SharpFetch` | Target Platforms | Active Detection Method | Notes |
| :--- | :---: | :---: | :--- | :--- |
| **OS Name & Edition** | 🟢 Implemented | Win / Linux / Mac | Win: Registry `CurrentVersion` / Lin: `/etc/os-release` / Mac: `kern.osproductversion` | Fast, in-memory read (< 0.05 ms) |
| **OS Version & Build Number** | 🟢 Implemented | Win / Linux / Mac | Win: `DisplayVersion` + `CurrentBuildNumber.UBR` / Lin: `VERSION_ID` / Mac: `kern.osversion` | e.g. `Windows 11 Pro (25H2, Build 26200.9168)` |
| **Kernel Name & Version** | 🟢 Implemented | Win / Linux / Mac | Win: `Environment.OSVersion.Version` / Lin: `/proc/sys/kernel/osrelease` / Mac: `Darwin {osrelease}` | e.g. `10.0.26200.0` |
| **Host Architecture** | 🟢 Implemented | Win / Linux / Mac | `RuntimeInformation.OSArchitecture` (`x86_64`, `arm64`, `aarch64`, `i686`) | Normalized standard names |
| **Hostname & Username** | 🟢 Implemented | Win / Linux / Mac | `Environment.MachineName`, `Environment.UserName` | Built-in .NET BCL |
| **System Uptime** | 🟢 Implemented | Win / Linux / Mac | Win: `Environment.TickCount64` / Lin: `/proc/uptime` / Mac: `kern.boottime` | Sub-microsecond execution |
| **System Boot Timestamp** | 🟢 Implemented | Win / Linux / Mac | `DateTime.UtcNow - TimeSpan.FromMilliseconds(TickCount64)` | Calculated locally |

---

## 2. Processor (CPU)

| Component / Metric | Current Status in `SharpFetch` | Target Platforms | Planned Detection Method | Notes |
| :--- | :---: | :---: | :--- | :--- |
| **CPU Model & Brand String** | 🔴 Pending | Win / Linux / Mac | Win: Registry `CentralProcessor\0` / Lin: `/proc/cpuinfo` / Mac: `machdep.cpu.brand_string` | e.g. `AMD Ryzen 7 7800X3D` |
| **CPU Vendor Identifier** | 🔴 Pending | Win / Linux / Mac | Win: `VendorIdentifier` / Lin: `vendor_id` / Mac: `machdep.cpu.vendor` | `AuthenticAMD`, `GenuineIntel`, `Apple` |
| **Base Clock Speed (GHz/MHz)**| 🔴 Pending | Win / Linux / Mac | Win: Registry `~MHz` / Lin: `/proc/cpuinfo` / Mac: `hw.cpufrequency` | Base factory clock |
| **Max / Boost Clock Speed** | 🔴 Pending | Win / Linux / Mac | Win: SMBIOS Type 4 / Lin: `cpuinfo_max_freq` / Mac: IOKit `pmgr` SRAM | High clock boost speed |
| **Physical Core Count** | 🔴 Pending | Win / Linux / Mac | Win: `GetLogicalProcessorInformationEx` / Lin: `/proc/cpuinfo` / Mac: `hw.physicalcpu` | Physical silicon cores |
| **Logical Threads Count** | 🔴 Pending | Win / Linux / Mac | `Environment.ProcessorCount` (Cross-platform) | Logical execution threads |
| **P-Core & E-Core Breakdown** | 🔴 Pending | Win / Linux / Mac | Win: `EfficiencyClass` / Lin: `topology/` / Mac: `hw.nperflevels` | Hybrid Intel 12th+ Gen & Apple Silicon |
| **L1 / L2 / L3 Cache Sizes** | 🔴 Pending | Win / Linux / Mac | Win: `RelationCache` / Lin: `/sys/.../cache/` / Mac: `hw.l2cachesize` | Cache hierarchy |
| **Real-time CPU Load %** | 🔴 Pending | Win / Linux / Mac | Win: `GetSystemTimes` / Lin: `/proc/stat` / Mac: `host_statistics64` | Instantaneous load measurement |
| **CPU Temperature** | 🔴 Pending | Win / Linux / Mac | Win: Perflib v2 `\ _TZ.CPUZ` / Lin: `/sys/class/hwmon` / Mac: IOKit SMC keys | Thermal sensors |

---

## 3. Graphics & Displays (GPU & Monitors)

| Component / Metric | Current Status in `SharpFetch` | Target Platforms | Planned Detection Method | Notes |
| :--- | :---: | :---: | :--- | :--- |
| **GPU Model & Vendor Name** | 🔴 Pending | Win / Linux / Mac | Win: `D3DKMT` / Registry / Lin: `/sys/bus/pci` / Mac: `Metal.framework` | NVIDIA, AMD Radeon, Intel Arc, Apple |
| **Multi-GPU Enumeration** | 🔴 Pending | Win / Linux / Mac | Win: `D3DKMTEnumAdapters2` / Lin: PCI sysfs / Mac: `MTLCopyAllDevices` | Dual iGPU + dGPU |
| **Dedicated VRAM Size** | 🔴 Pending | Win / Linux / Mac | Win: `D3DKMT_SEGMENTSIZEINFO` / Lin: sysfs / Mac: Metal unified | Dedicated GPU memory |
| **Shared System VRAM Size** | 🔴 Pending | Win / Linux / Mac | Win: `D3DKMT` shared segment / Lin: kernel driver / Mac: Metal max working set | System RAM allocatable to GPU |
| **Live VRAM Usage** | 🔴 Pending | Win / Linux | Win: `D3DKMT_QUERYSTATISTICS_SEGMENT_GROUP_USAGE` (Win 11 22H2+) | Real-time memory allocation |
| **GPU Driver Version** | 🔴 Pending | Win / Linux / Mac | Win: `D3DKMT_UMD_DRIVER_VERSION` / Lin: sysfs `driver/module` / Mac: Kext info | Installed display driver |
| **Platform API (WDDM / Metal)**| 🔴 Pending | Win / Linux / Mac | Win: `D3DKMT` WDDM version / Lin: DRM/Mesa / Mac: Metal Feature Set | Graphics subsystem level |
| **Connected Monitor Names** | 🟢 Implemented | Win / Linux / Mac | Win: `QueryDisplayConfig` + Registry EDID / Lin: `/sys/class/drm/*/edid` / Mac: CoreGraphics | Decoded friendly monitor strings (e.g. `SMB2030`) |
| **Active Screen Resolution** | 🟢 Implemented | Win / Linux / Mac | Win: `user32.dll` EnumDisplaySettings / Lin: DRM modes / Mac: `CGDisplayBounds` | e.g. `1600x900` |
| **Refresh Rate (Hz)** | 🟢 Implemented | Win / Linux / Mac | Win: `EnumDisplaySettings` / `QueryDisplayConfig` / Lin: EDID / Mac: `CGDisplayModeGetRefreshRate` | e.g. `60 Hz`, `144 Hz` |
| **Monitor Diagonal Size** | 🟢 Implemented | Win / Linux / Mac | VESA EDID physical dimensions parser | e.g. `in 20"`, `in 27"` |
| **Display Type (Built-in / External)**| 🟢 Implemented | Win / Linux / Mac | Win: `outputTechnology` / Lin: `eDP` vs `HDMI`/`DP` | `[Built-in]` vs `[External]` |

---

## 4. Memory (RAM) & Swap

| Component / Metric | Current Status in `SharpFetch` | Target Platforms | Planned Detection Method | Notes |
| :--- | :---: | :---: | :--- | :--- |
| **Total Physical RAM** | 🔴 Pending | Win / Linux / Mac | Win: `GlobalMemoryStatusEx` / Lin: `/proc/meminfo` / Mac: `hw.memsize` | Total installed RAM |
| **Available / Free Physical RAM**| 🔴 Pending | Win / Linux / Mac | Win: `ullAvailPhys` / Lin: `MemAvailable` / Mac: `host_statistics64` | Real-time unallocated RAM |
| **Used Physical RAM** | 🔴 Pending | Win / Linux / Mac | Total - Available (ZFS ARC adjusted on Linux) | Used memory for active processes |
| **Memory Usage Percentage** | 🔴 Pending | Win / Linux / Mac | `(Used / Total) * 100` | Rendered with visual Spectre progress bar |
| **Memory Form Factor & Type** | 🔴 Pending | Win / Linux | Win: SMBIOS Type 17 / Lin: `/sys/firmware/dmi/tables/DMI` | `DIMM`, `DDR4`, `DDR5` |
| **Memory Speed (MT/s / MHz)** | 🔴 Pending | Win / Linux | Win: SMBIOS Type 17 `ConfiguredMemoryClockSpeed` / Lin: DMI | e.g. `6000 MT/s` |
| **Total Swap / Pagefile Size**| 🔴 Pending | Win / Linux / Mac | Win: `ullTotalPageFile` / Lin: `SwapTotal` / Mac: `vm_stat` | Virtual memory paging |
| **Used Swap / Pagefile Size** | 🔴 Pending | Win / Linux / Mac | Win: `ullTotalPageFile - ullAvailPageFile` / Lin: `SwapTotal - SwapFree` | Active pagefile pressure |

---

## 5. Storage & Filesystem Drives

| Component / Metric | Current Status in `SharpFetch` | Target Platforms | Planned Detection Method | Notes |
| :--- | :---: | :---: | :--- | :--- |
| **Mounted Drives / Volumes** | 🔴 Pending | Win / Linux / Mac | `System.IO.DriveInfo.GetDrives()` (Cross-platform) | `C:\`, `D:\`, `/`, `/home` |
| **Volume Total Capacity** | 🔴 Pending | Win / Linux / Mac | `DriveInfo.TotalSize` | Filesystem partition capacity |
| **Volume Free & Used Space** | 🔴 Pending | Win / Linux / Mac | `DriveInfo.AvailableFreeSpace` | Free space available to user |
| **Volume Storage Usage %** | 🔴 Pending | Win / Linux / Mac | `((Total - Free) / Total) * 100` | Rendered with visual disk usage widget |
| **Volume Filesystem Format** | 🔴 Pending | Win / Linux / Mac | `DriveInfo.DriveFormat` | `NTFS`, `ext4`, `btrfs`, `APFS` |
| **Physical Disk Model & Bus** | 🔴 Pending | Win / Linux / Mac | Win: `IOCTL_STORAGE_QUERY_PROPERTY` / Lin: `/sys/block/*/device/model` | e.g. `Samsung SSD 990 PRO 2TB` |
| **Disk Media Type (NVMe/SSD/HDD)**| 🔴 Pending | Win / Linux / Mac | Win: `STORAGE_PROPERTY_QUERY` / Lin: `/sys/block/*/queue/rotational` | NVMe, SATA SSD, Rotational HDD |

---

## 6. Motherboard, BIOS & Computer Host

| Component / Metric | Current Status in `SharpFetch` | Target Platforms | Planned Detection Method | Notes |
| :--- | :---: | :---: | :--- | :--- |
| **Computer Host System Model** | 🔴 Pending | Win / Linux / Mac | Win: Registry `System\BIOS` or SMBIOS Type 1 / Lin: `/sys/class/dmi/id/product_name` / Mac: `hw.model` | e.g. `ROG Strix G16` |
| **Computer Manufacturer** | 🔴 Pending | Win / Linux / Mac | Win: Registry `SystemManufacturer` / Lin: `/sys/class/dmi/id/sys_vendor` / Mac: `Apple Inc.` | ASUS, Dell, Lenovo, HP, Apple |
| **Motherboard Model & Vendor**| 🔴 Pending | Win / Linux / Mac | Win: Registry `BaseBoardProduct` / Lin: `/sys/class/dmi/id/board_name` | Motherboard identifier |
| **BIOS / UEFI Version & Date**| 🔴 Pending | Win / Linux / Mac | Win: Registry `BIOSVersion` / Lin: `/sys/class/dmi/id/bios_version` | e.g. `F21a (05/24/2024)` |
| **Chassis / Form Factor** | 🔴 Pending | Win / Linux / Mac | Win: SMBIOS Type 3 / Lin: `/sys/class/dmi/id/chassis_type` / Mac: Model logic | `Desktop`, `Laptop`, `Notebook` |
| **System UUID** | 🔴 Pending | Win / Linux / Mac | Win: SMBIOS Type 1 UUID / Lin: `/sys/class/dmi/id/product_uuid` / Mac: `IOPlatformUUID` | Hardware unique identifier |

---

## 7. Power & Battery

| Component / Metric | Current Status in `SharpFetch` | Target Platforms | Planned Detection Method | Notes |
| :--- | :---: | :---: | :--- | :--- |
| **Battery Presence Check** | 🔴 Pending | Win / Linux / Mac | Win: `GetSystemPowerStatus` / Lin: `/sys/class/power_supply/BAT*` / Mac: `IOPSCopyPowerSourcesInfo` | Automatically hides on desktop PCs |
| **Battery Charge Percentage** | 🔴 Pending | Win / Linux / Mac | Win: `BatteryLifePercent` / Lin: `/sys/.../capacity` / Mac: `IOPSGetPowerSourceDescription` | e.g. `87%` |
| **Power State (AC / Battery)** | 🔴 Pending | Win / Linux / Mac | Win: `ACLineStatus` / Lin: `status` (`Charging`, `Discharging`, `Full`) / Mac: `IsCharging` | Plugged in vs Discharging |
| **Time Remaining on Battery** | 🔴 Pending | Win / Linux / Mac | Win: `BatteryLifeTime` / Lin: `energy_now / power_now` / Mac: `Time to Empty` | Estimated remaining battery life |
| **Battery Health & Cycle Count**| 🔴 Pending | Win / Linux / Mac | Win: `IOCTL_BATTERY_QUERY_INFORMATION` / Lin: `energy_full_design` / Mac: IOKit | Health degradation % & cycles |

---

## 8. Network & Connectivity

| Component / Metric | Current Status in `SharpFetch` | Target Platforms | Planned Detection Method | Notes |
| :--- | :---: | :---: | :--- | :--- |
| **Active Network Adapters** | 🔴 Pending | Win / Linux / Mac | `System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()` | Built-in .NET BCL |
| **MAC Addresses** | 🔴 Pending | Win / Linux / Mac | `NetworkInterface.GetPhysicalAddress()` | Formatted as `AA:BB:CC:DD:EE:FF` |
| **Local IPv4 & IPv6 Addresses**| 🔴 Pending | Win / Linux / Mac | `NetworkInterface.GetIPProperties().UnicastAddresses` | Local subnet IP addresses |
| **Default IP Gateway** | 🔴 Pending | Win / Linux / Mac | `NetworkInterface.GetIPProperties().GatewayAddresses` | Default router gateway |
| **Interface Link Speed** | 🔴 Pending | Win / Linux / Mac | `NetworkInterface.Speed` (bps to Gbps/Mbps) | e.g. `1 Gbps`, `2.5 Gbps` |
| **Public IP Address** | 🔴 Pending | Win / Linux / Mac | Optional HTTP query (e.g. `https://api.ipify.org`) | Off by default (privacy & offline mode) |

---

## 9. Environment, Terminal, Shell & Packages

| Component / Metric | Current Status in `SharpFetch` | Target Platforms | Planned Detection Method | Notes |
| :--- | :---: | :--- | :--- | :--- |
| **Shell Name & Version** | 🔴 Pending | Win / Linux / Mac | Process tree PPID inspection (`pwsh`, `powershell`, `cmd`, `bash`, `zsh`, `nu`, `fish`) | Identifies active shell |
| **Terminal Emulator** | 🔴 Pending | Win / Linux / Mac | Env: `WT_SESSION`, `ALACRITTY_LOG`, `TERM_PROGRAM`, `ConEmuPID`, `TERM` + PPID | Windows Terminal, Alacritty, VS Code, etc. |
| **System Theme (Dark/Light)** | 🔴 Pending | Win / Linux / Mac | Win: Registry `Themes\Personalize` / Lin: `gsettings` / Mac: `AppleInterfaceStyle` | Dark mode vs Light mode |
| **Winget / Scoop / Choco Count**| 🔴 Pending | Windows | Directory scan in `%USERPROFILE%\scoop\apps` & `%ChocolateyInstall%\lib` | Fast directory scan (< 0.5 ms) |
| **Pacman / APT / Homebrew Count**| 🔴 Pending | Linux / macOS | Directory scan in `/var/lib/pacman/local`, `/var/lib/dpkg/status`, `/opt/homebrew` | Fast directory scan (< 0.5 ms) |
| **.NET Runtime & SDK Version**| 🔴 Pending | Win / Linux / Mac | `RuntimeInformation.FrameworkDescription` (`.NET 9.0.x`) | Built-in .NET BCL |

---

## 10. Project Implementation Progress Tracker

```
+---------------------------------------------------------------------------------------------------+
|                                 SHARPFETCH IMPLEMENTATION PROGRESS                                |
+---------------------------------------------------------------------------------------------------+
| [*] Implemented & Active:    16 / 74 Modules  (21.6%)                                             |
| [.] In Progress / Partial:    2 / 74 Modules  ( 2.7%)                                             |
| [ ] Pending:                 56 / 74 Modules  (75.7%)                                             |
+---------------------------------------------------------------------------------------------------+
```

