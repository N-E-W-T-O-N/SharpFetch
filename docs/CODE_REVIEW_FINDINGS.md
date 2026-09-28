# Code Review Findings — Devops branch (2026-09-28)

Review scope: current uncommitted work on `Devops` (Battery/Host modules and
probes, FreeBSD probes, macOS Memory/Swap probes, CLI parser, ConsoleRenderer,
LinuxGpuProbe). Build/tests/CLI run all pass at time of review (222 tests,
0 errors/warnings) — these are correctness findings, not build breakers.

Not yet fixed. Ordered by severity.

## 1. `MacOsMemoryProbe.GetUsedBytes()` reads the wrong struct offset

File: `src/SharpFetch.Core/Platforms/MacOS/MacOsMemoryProbe.cs` (~line 86-91)

Reads `vmStat[18]` and labels it `compressor_page_count`. Verified against
Apple's real `vm_statistics64` struct (`apple-oss-distributions/xnu`,
`osfmk/mach/vm_statistics.h`): computing exact 32-bit word offsets from the
field list (free_count, active_count, inactive_count, wire_count, then a run
of uint64 fields, then purgeable_count/speculative_count, more uint64 fields,
then compressor_page_count/throttled_count/external_page_count/
internal_page_count) puts `compressor_page_count` at **word 32**, not 18.
Word 18 is actually the low 32 bits of `hits` (a lifetime VM-lookup counter
with no relationship to memory usage).

Effect: on real macOS hardware, `GetUsedBytes()` adds a nonsense page count
into the used-memory total. The caller's `usedBytes > totalBytes` clamp masks
the worst case by pinning the display to 100%, so this fails silently rather
than crashing — the reported "used" percentage will just be wrong.

Fix: change `vmStat[18]` to `vmStat[32]`.

## 2. `PrintModuleList()` is stale (Program.cs)

File: `src/SharpFetch.Cli/Program.cs` (~line 102-116)

Hardcoded string listing only 9 modules (Title/OS/Kernel/Uptime/CPU/GPU/
Memory/Swap/Disk). Host, LocalIp, Wifi, Battery, and Display were added later
and never wired into this method — `sharpfetch --list-modules` gives an
actively wrong answer about what modules exist.

Fix: generate the list from `FetchEngine.CreateDefault().Modules` metadata
instead of a hand-maintained string literal, so it can't drift again.

## 3. Unescaped username/hostname in Spectre markup (ConsoleRenderer)

File: `src/SharpFetch.UI/ConsoleRenderer.cs` (~line 37-43)

The Title row splits the OS-reported `user@host` string and interpolates
`user`/`host` directly into Spectre markup without `Markup.Escape`. Every
other row in the same method (line 59) does escape its interpolated value.
`AsciiArt.ValidateColor` already guards a different user-influenced string
(the `--color` CLI option) with a try/catch specifically to avoid this class
of crash — this path was missed.

Effect: a username or hostname containing `[` would throw inside Spectre's
markup parser, crashing the whole render step (not just producing garbled
output).

Fix: wrap `user`/`host`/`rawTitle` in `Markup.Escape(...)` at the two
interpolation sites, matching line 59's pattern.

## 4. `HostModule.DefaultOrder` collides with `OsModule.DefaultOrder` (both 2)

Files: `src/SharpFetch.Core/Modules/HostModule.cs` (line 19),
`src/SharpFetch.Core/Modules/OsModule.cs` (line 19)

Every other module has a unique `DefaultOrder` value (1, 3, 4, 5, 6, 7, 8, 9,
11, 21, 37, 72). This is the only duplicate. It currently "works" only
because LINQ's `OrderBy` is a stable sort and preserves `FetchEngine`'s
registration order (Host registered before Os) as the tie-break — fragile,
not an explicit ordering.

Fix: give `HostModule` its own order value (e.g. between Title=1 and OS=2,
or renumber into an unused slot).

## 5. `FreeBSDHostProbe.Detect()` only handles the VM-guest case

File: `src/SharpFetch.Core/Platforms/FreeBSDHostProbe.cs`

Only returns a result when `kern.vm_guest` != "none" (i.e. running inside a
VM). On bare-metal FreeBSD it returns `null` unconditionally — no board/
vendor detection at all, unlike Windows/Linux which read real SMBIOS-derived
info (registry / `/sys/class/dmi/id`). FreeBSD's equivalent is `kenv`'s
SMBIOS variables (`smbios.system.product`, `smbios.system.maker`,
`smbios.planar.product`, `smbios.planar.maker`).

Not a crash — just an OS-coverage gap on an already best-effort platform.

## 6. Minor: `NetworkProbe.IsVirtualInterface` overly broad "wg" substring match

File: `src/SharpFetch.Core/Platforms/Common/NetworkProbe.cs` (~line 187)

`nameLower.Contains("wg")` is meant to catch WireGuard interfaces but is
broad enough to false-positive on any real adapter name that happens to
contain those two letters.

## ARM GPU naming — real, low-risk gap (not a bug, an accuracy shortfall)

File: `src/SharpFetch.Core/Platforms/LinuxGpuProbe.cs`, `DetectSocGpu()`
(~line 80-139)

Checked against fastfetch's real upstream source
(`fastfetch-cli/fastfetch`, `src/detection/gpu/gpu_linux.c`,
`detectOf()`): fastfetch does NOT identify ARM SoC GPUs by kernel driver
name. It reads `/sys/class/drm/cardN/device/modalias`, and when it's an
Open Firmware (`of:`) devicetree modalias, parses the `compatible` field
itself (e.g. `qcom,adreno-630`, `arm,mali-t860`) to get a real chip model.
It only falls back to driver-name matching for a couple of special cases
(e.g. `asahi` on Apple Silicon).

SharpFetch's `DetectSocGpu()` only does the driver-name → generic-vendor
mapping (`panfrost`/`mali` → "ARM Mali GPU", `msm` → "Qualcomm Adreno"), with
no chip model number at all.

Suggested fix (same risk tier as the rest of this file — plain sysfs text
read, no ioctls): parse the `of:` modalias's `compatible` field as the
primary path, falling back to the existing driver-name table when no
devicetree modalias exists (non-devicetree ARM boards, or DRM devices that
already went through the primary PCI path).

## Verification performed

- `dotnet build SharpFetch.slnx -c Release --no-incremental`: 0 errors, 0 warnings
- `dotnet test SharpFetch.slnx -c Release --no-build`: 222/222 passed
- `dotnet run` on this Windows machine: renders correctly (Host, OS, Kernel,
  CPU, GPU, Memory, Swap, Disk x2, Uptime, Display, Network all present and
  correct)
