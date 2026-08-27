# SharpFetch Component & Device Support Checklist

> **Current Implementation Status**: 🟢 **15% Implemented (Core OS, Architecture, Kernel & Uptime Active)**  
> This checklist tracks what hardware and system telemetry components `SharpFetch` can read and display.

### Status Legend
* 🟢 **Implemented & Verified**: Code is implemented, tested, and actively working in `SharpFetch`.
* 🟡 **In Progress / Partial**: Basic stub or partial platform support written; requires refinement or testing.
* 🔴 **Not Implemented / Empty**: Not yet implemented in the codebase (pending).

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
| **PCIe Link Speed & Lanes** | 🔴 Pending | Win / Linux | Win: `cfgmgr32.dll` (`DEVPKEY_PciDevice_CurrentLinkSpeed`) / Lin: sysfs | e.g. `PCIe 4.0 x16` |
| **Connected Monitor Names** | 🔴 Pending | Win / Linux / Mac | Win: `QueryDisplayConfig` EDID / Lin: `/sys/class/drm/*/edid` / Mac: CoreGraphics | Decoded friendly monitor strings |
| **Active Screen Resolution** | 🔴 Pending | Win / Linux / Mac | Win: `user32.dll` / Lin: DRM modes / Mac: `CGDisplayBounds` | e.g. `2560x1440` |
| **Refresh Rate (Hz)** | 🔴 Pending | Win / Linux / Mac | Win: `QueryDisplayConfig` / Lin: EDID / Mac: `CGDisplayModeGetRefreshRate` | e.g. `144 Hz` |

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
| 🟢 Implemented:    7 / 48 Components  (15%)                                                        |
| 🟡 In Progress:    0 / 48 Components  (0%)                                                         |
| 🔴 Pending/Empty: 41 / 48 Components  (85%)                                                        |
+---------------------------------------------------------------------------------------------------+
```
