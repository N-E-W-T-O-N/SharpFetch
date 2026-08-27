namespace SharpFetch.Core.Models;

public sealed record OsInfo
{
    /// <summary>
    /// Operating system commercial name (e.g. "Windows 11 Pro", "Arch Linux", "Ubuntu", "macOS")
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// OS Version string (e.g. "24H2", "24.04", "15.3")
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// OS Build identifier or revision (e.g. "26100.3037", "24D60")
    /// </summary>
    public required string Build { get; init; }

    /// <summary>
    /// Kernel name and version (e.g. "10.0.26100.0", "6.12.10-arch1-1", "Darwin 24.3.0")
    /// </summary>
    public required string Kernel { get; init; }

    /// <summary>
    /// Normalized architecture (e.g. "x86_64", "arm64", "x86", "arm")
    /// </summary>
    public required string Architecture { get; init; }

    /// <summary>
    /// Operating system family enum (Windows, Linux, MacOS, Unknown)
    /// </summary>
    public required OsFamily Family { get; init; }

    /// <summary>
    /// Optional distro/release codename (e.g. "Sequoia", "Noble Numbat")
    /// </summary>
    public string? Codename { get; init; }

    /// <summary>
    /// Machine hostname
    /// </summary>
    public required string Hostname { get; init; }

    /// <summary>
    /// Current logged-in username
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// System uptime duration
    /// </summary>
    public required TimeSpan Uptime { get; init; }

    /// <summary>
    /// System boot timestamp
    /// </summary>
    public DateTimeOffset BootTime => DateTimeOffset.UtcNow - Uptime;

    /// <summary>
    /// Pretty formatted name (e.g. "Windows 11 Pro x86_64 (24H2, Build 26100.3037)")
    /// </summary>
    public string PrettyName
    {
        get
        {
            if (Family == OsFamily.Windows)
            {
                string verPart = !string.IsNullOrWhiteSpace(Version) ? $"{Version}" : "";
                string buildPart = !string.IsNullOrWhiteSpace(Build) ? $"Build {Build}" : "";
                string details = string.Join(", ", new[] { verPart, buildPart }.Where(s => !string.IsNullOrEmpty(s)));
                return string.IsNullOrEmpty(details) ? Name : $"{Name} ({details})";
            }

            if (!string.IsNullOrWhiteSpace(Codename))
            {
                return $"{Name} {Version} ({Codename})";
            }

            if (!string.IsNullOrWhiteSpace(Version))
            {
                return $"{Name} {Version}";
            }

            return Name;
        }
    }
}
