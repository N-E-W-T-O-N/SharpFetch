First release of **SharpFetch**, a cross-platform command-line system information tool written in C#. It reads hardware and operating system details through native platform probes and prints a compact terminal report.

## What is covered

Fourteen report modules. Which ones appear depends on the operating system, the hardware, and connection state (for example, Battery is omitted on a desktop, Wi-Fi when no wireless link is up).

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

Also included: built-in ASCII logos, color output, module selection (`--modules`, `--hide`, `--list-modules`) and `--details` for full network adapter information. See [Support.md](https://github.com/N-E-W-T-O-N/SharpFetch/blob/v1.0.1/Support.md) for the full checklist.

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
