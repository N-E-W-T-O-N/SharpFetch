# SharpFetch

SharpFetch is a cross-platform command-line system information tool written in C#. It collects hardware and operating system details through platform-specific probes and displays them in a compact terminal report.

## Features

- Operating system, host model, kernel, CPU, and GPU details
- Memory, swap, disk, display, and battery information
- Network addresses and connected Wi-Fi details
- Built-in ASCII logos, color output, and selectable report modules
- Native AOT-compatible project configuration

Available information depends on the operating system, hardware, and connection state. See the [support checklist](Support.md) for platform-specific coverage.

## Requirements

- .NET SDK **10.0.400** (selected by [`global.json`](global.json))
- Windows, Linux, or macOS for the corresponding platform probes

The projects target .NET 8, 9, and 10. The repository pins the .NET 10 SDK for building the full solution.

## Download

Prebuilt binaries are published on the [GitHub Releases page](https://github.com/N-E-W-T-O-N/SharpFetch/releases). Choose the archive matching your operating system and processor architecture, then extract it and run `SharpFetch` (or `SharpFetch.exe` on Windows). Each archive includes this README and the Apache-2.0 license.

Native AOT builds do not require the .NET runtime. The Linux ARM 32-bit build is a self-contained single-file ReadyToRun build (not AOT) and also needs no separate .NET install. The portable archive is framework-dependent and requires .NET 10.0 or later. Linux builds target glibc unless the archive name contains `musl` (for Alpine Linux).

The Windows archive is a `.zip`; Linux and macOS archives are `.tar.gz` files. See the [support checklist](Support.md) for platform-specific details.

## Build and run

From the repository root, build the solution:

```sh
dotnet build SharpFetch.slnx -c Release
```

Run SharpFetch from source:

```sh
dotnet run --project src/SharpFetch.Cli/SharpFetch.Cli.csproj --framework net10.0
```

Show help or list the available modules:

```sh
dotnet run --project src/SharpFetch.Cli/SharpFetch.Cli.csproj --framework net10.0 -- --help
dotnet run --project src/SharpFetch.Cli/SharpFetch.Cli.csproj --framework net10.0 -- --list-modules
```

## Usage

```sh
# Show selected modules without the logo
dotnet run --project src/SharpFetch.Cli/SharpFetch.Cli.csproj --framework net10.0 -- --modules os,cpu,memory --no-logo

# Include IPv6, MAC address, adapter speed, and module explanations
dotnet run --project src/SharpFetch.Cli/SharpFetch.Cli.csproj --framework net10.0 -- --details

# Hide selected modules and disable terminal colors
dotnet run --project src/SharpFetch.Cli/SharpFetch.Cli.csproj --framework net10.0 -- --hide gpu,battery --no-color
```

| Option | Description |
| --- | --- |
| `-h`, `--help` | Show command help |
| `-v`, `--version` | Show the SharpFetch version |
| `--list-modules` | List module names and their descriptions |
| `-m`, `--modules <list>` | Show a comma-separated list of modules (for example, `os,cpu,memory`) |
| `--hide <list>` | Hide a comma-separated list of modules |
| `--details` | Show full network adapter details and module explanations |
| `-n`, `--no-logo` | Hide the ASCII logo |
| `--no-color` | Disable ANSI colors |
| `--no-palette` | Hide the terminal color palette |
| `-l`, `--logo <name>` | Select a built-in logo |
| `--color <name>` | Set the accent color |

## Documentation

- [Platform support checklist](Support.md)
- [Build prerequisites and native interoperability](docs/PREREQUISITES.md)
- [Dependencies and implementation strategy](docs/DEPENDENCIES.md)
- [Windows performance analysis](docs/WINDOWS_PERFORMANCE_COMPARISON.md)
- [Linux and macOS performance analysis](docs/LINUX_MACOS_PERFORMANCE_COMPARISON.md)
- [.NET ecosystem analysis](docs/DOTNET_ECOSYSTEM_ANALYSIS.md)
- [Architecture and implementation observations](OBSERVATIONS.md)

## License

SharpFetch is licensed under the Apache License 2.0. You may use and redistribute it, including in commercial products, subject to the license terms and preservation of applicable copyright and license notices. See [LICENSE](LICENSE) for the complete terms.
