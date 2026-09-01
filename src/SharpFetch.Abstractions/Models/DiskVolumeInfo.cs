namespace SharpFetch.Core.Models;

public sealed record DiskVolumeInfo
{
    /// <summary>
    /// Mount point / drive root as the OS names it (e.g. "C:\", "/", "/home")
    /// </summary>
    public required string MountPoint { get; init; }

    /// <summary>
    /// Filesystem type (e.g. "NTFS", "ext4", "APFS")
    /// </summary>
    public required string FileSystem { get; init; }

    public required ulong TotalBytes { get; init; }
    public required ulong UsedBytes { get; init; }
}
