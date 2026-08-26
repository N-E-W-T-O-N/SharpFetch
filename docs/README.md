# SharpFetch Documentation

Welcome to the **SharpFetch** system architecture, support matrix, and performance analysis documentation.

## Documents

1. [**Support.md**](file:///D:/Projects/SharpFetch/Support.md) / [**docs/SUPPORT.md**](file:///D:/Projects/SharpFetch/docs/SUPPORT.md)
   - Complete hardware, system telemetry, and device support checklist.
   - Categorized by 🟢 **Supported**, 🟡 **Partial / Conditional**, and 🔴 **Not Supported / Unavailable** across Windows, Linux, and macOS.
   - Details exact detection methods, APIs, and notes for all 10 hardware subsystems.

2. [**OBSERVATIONS.md**](file:///D:/Projects/SharpFetch/docs/OBSERVATIONS.md)
   - Deep architectural and low-level analysis comparing **`fastfetch`**, **`neofetch`**, **`winfetch`**, and **`Hardware.Info`**.
   - Covers operating system mechanisms: virtual filesystems (`/proc`, `/sys`), kernel syscalls, `sysctl`, Windows Registry, Win32 P/Invoke, SMBIOS, SetupAPI/D3DKMT, and WMI/CIM.
   - Comprehensive telemetry breakdown for OS, CPU, GPU, Memory, Storage, Battery, Motherboard, Displays, Network, Uptime, Terminal, Shell, and Packages.
   - Design trade-offs and high-performance .NET 9 CLI implementation blueprint.

3. [**WINDOWS_PERFORMANCE_COMPARISON.md**](file:///D:/Projects/SharpFetch/docs/WINDOWS_PERFORMANCE_COMPARISON.md)
   - In-depth technical breakdown and benchmark analysis on **Windows**.
   - Explains why `Hardware.Info` is slow (WMI COM overhead + `Task.Delay`) and how `fastfetch` achieves sub-millisecond speeds (Registry, Win32 P/Invoke, D3DKMT, raw SMBIOS tables).
   - Includes ready-to-use C# (.NET 9) code examples for `SharpFetch`.

4. [**LINUX_MACOS_PERFORMANCE_COMPARISON.md**](file:///D:/Projects/SharpFetch/docs/LINUX_MACOS_PERFORMANCE_COMPARISON.md)
   - In-depth technical breakdown and benchmark analysis on **Linux** and **macOS**.
   - Explains why `Hardware.Info` and `neofetch` are slow on Linux/macOS (spawning `lshw`, `lspci`, `system_profiler`, `vm_stat`, shell subshells) and how `fastfetch` achieves 1–4 ms speeds via in-process procfs/sysfs parsing, IOKit, Mach kernel APIs, and `sysctlbyname`.
   - Includes ready-to-use C# (.NET 9) `sysctl` and `/proc` zero-allocation span parsing patterns for `SharpFetch`.

5. [**DOTNET_ECOSYSTEM_ANALYSIS.md**](file:///D:/Projects/SharpFetch/docs/DOTNET_ECOSYSTEM_ANALYSIS.md)
   - Comprehensive analysis of existing C# / .NET fetch tools (`netfetch`, `Windows-Fetch`) and telemetry libraries (`CZGL.SystemInfo`, `LibreHardwareMonitor`, `Hardware.Info`).
   - Identifies historical pitfalls in older C# fetch tools (WMI bottleneck, subprocess spawning, JIT cold starts) and how modern .NET 9 + Native AOT + `Spectre.Console` overcomes them to build a world-class CLI tool.
