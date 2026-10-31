SharpFetch **v1.2** fixes multi-monitor detection on every platform, cleans up the disk report, gives Linux GPUs real model names, and stops macOS helper commands from hanging the program.

## Fixed

- **Every active display is detected.** A laptop screen plus an external monitor often showed as one display.
  - **Windows:** displays now come from `QueryDisplayConfig`, one per physical screen, the same source fastfetch uses. Screens in Duplicate mode are listed separately. Each screen reports its own name, size, resolution, real refresh rate (for example 143.98 Hz), built-in/external type and primary flag.
  - **Linux:** active monitors now come from the X server (XRandR, also available through XWayland), with the kernel's DRM connectors as a fallback. This reports the current resolution and refresh rate, and finds monitors on drivers without DRM connectors. Monitors turned off in display settings are no longer listed.
  - **macOS:** only the main display is marked primary.
- **The disk report shows real disks only.** Kernel filesystems (efivarfs, tracefs, rpc_pipefs, …), snap squashfs images, FUSE helper mounts and hidden macOS system volumes are no longer listed as disks.
- **macOS helper commands can no longer freeze SharpFetch.** `system_profiler` and `pmset` now really stop at their time limit, and `system_profiler SPDisplaysDataType` runs once per fetch instead of twice.

## Improved

- **Linux GPU names:** when the system `pci.ids` database is installed (hwdata/pciutils), GPUs show vendor and model names the way `lspci` does, for example `NVIDIA Corporation AD102 [GeForce RTX 4090]`.
- **Code layout:** platform code now lives in per-OS folders (`Platforms/Common`, `Linux`, `MacOS`, `Windows`, `FreeBSD`, `Android`) with matching namespaces.

## What is covered

| Module | Windows | Linux | macOS | FreeBSD |
|---|:-:|:-:|:-:|:-:|
| Title, OS, Kernel, Uptime | Yes | Yes | Yes | Yes |
| Host (computer model) | Yes | Yes | Yes | Yes |
| CPU | Yes | Yes | Yes | Yes |
| GPU | Yes | Yes | Yes | - |
| Display | Yes | Yes | Yes | - |
| Memory | Yes | Yes | Yes | Yes |
| Swap | Yes | Yes | Yes | - |
| Disk | Yes | Yes | Yes | Yes |
| Network | Yes | Yes | Yes | Yes |
| Wi-Fi | Yes | Yes | Yes | - |
| Battery | Yes | Yes | Yes | Yes |

Which modules appear depends on the operating system, the hardware, and connection state. See [Support.md](https://github.com/N-E-W-T-O-N/SharpFetch/blob/v1.2/Support.md) for the full checklist.

## Downloads

| Platform | Archive |
|---|---|
| Windows x64 / ARM64 | `sharpfetch-win-x64.zip`, `sharpfetch-win-arm64.zip` |
| Linux x64 / ARM64 (glibc) | `sharpfetch-linux-x64.tar.gz`, `sharpfetch-linux-arm64.tar.gz` |
| Linux ARM 32-bit | `sharpfetch-linux-arm.tar.gz` (single-file ReadyToRun build, not AOT) |
| Alpine Linux x64 / ARM64 (musl) | `sharpfetch-linux-musl-x64.tar.gz`, `sharpfetch-linux-musl-arm64.tar.gz` |
| macOS Intel / Apple Silicon | `sharpfetch-osx-x64.tar.gz`, `sharpfetch-osx-arm64.tar.gz` |
| Any other .NET-supported OS (including FreeBSD) | `sharpfetch-portable.tar.gz` (needs the .NET 10 runtime) |

Extract an archive and run `SharpFetch` (`SharpFetch.exe` on Windows). The Native AOT builds need no .NET runtime.
