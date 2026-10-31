using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms.Common;

/// <summary>
/// System.IO.DriveInfo is genuinely cross-platform BCL - unlike CPU/GPU/OS
/// detection, disk usage needs no P/Invoke or per-OS code, so there is only
/// one implementation of this probe rather than one per platform.
/// </summary>
public sealed class DriveInfoDiskProbe : IDiskProbe
{
    /// <summary>
    /// True when a volume is worth reporting: non-empty, not a kernel/virtual
    /// filesystem, not a hidden macOS system volume, and - when the mount's
    /// source is known (Linux /proc/mounts) - backed by a real device or file
    /// rather than a placeholder like "none" or "sysfs". The root mount is
    /// always kept so containers on overlay still report their filesystem.
    /// </summary>
    public static bool IsReportable(string mountPoint, string fileSystem, long totalBytes, string? source = null)
    {
        if (totalBytes <= 0) return false;
        if (PseudoFileSystems.Contains(fileSystem)) return false;

        // FUSE helper mounts (fuse.portal, fuse.gvfsd-fuse, fuse.sshfs) are
        // desktop plumbing or network shares; fuseblk (ntfs-3g, exfat) is a real disk.
        if (fileSystem.StartsWith("fuse.", StringComparison.OrdinalIgnoreCase)) return false;

        // APFS splits one disk into many system volumes (VM, Preboot, Update,
        // Data, ...) that all mirror the "/" container's space.
        if (mountPoint.StartsWith("/System/Volumes/", StringComparison.Ordinal)) return false;

        return source is null || mountPoint == "/" || source.StartsWith('/');
    }

    /// <summary>
    /// Maps each mount point in /proc/mounts to its source device. Fields use
    /// octal escapes for whitespace (e.g. "\040" for a space). Later entries
    /// win, matching the mount that is visible on top of a stack.
    /// </summary>
    internal static Dictionary<string, string> ParseMountSources(IEnumerable<string> lines)
    {
        var sources = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string line in lines)
        {
            string[] fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length < 3) continue;

            sources[Unescape(fields[1])] = Unescape(fields[0]);
        }

        return sources;
    }

    private static string Unescape(string field)
    {
        if (!field.Contains('\\')) return field;

        var builder = new System.Text.StringBuilder(field.Length);
        for (int i = 0; i < field.Length; i++)
        {
            if (field[i] == '\\' && i + 3 < field.Length &&
                field[i + 1] is >= '0' and <= '7' && field[i + 2] is >= '0' and <= '7' && field[i + 3] is >= '0' and <= '7')
            {
                builder.Append((char)(((field[i + 1] - '0') << 6) | ((field[i + 2] - '0') << 3) | (field[i + 3] - '0')));
                i += 3;
            }
            else
            {
                builder.Append(field[i]);
            }
        }

        return builder.ToString();
    }

    private static Dictionary<string, string>? ReadMountSources()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsAndroid()) return null;

        try
        {
            return ParseMountSources(File.ReadLines("/proc/mounts"));
        }
        catch
        {
            return null;
        }
    }

    public IReadOnlyList<DiskVolumeInfo> Detect()
    {
        var volumes = new List<DiskVolumeInfo>();
        Dictionary<string, string>? mountSources = ReadMountSources();

        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            try
            {
                // Only fixed local volumes by default - removable/network/CD-ROM
                // drives are typically not what a system-fetch summary is for,
                // and network drives in particular can hang on a slow query.
                if (drive.DriveType != DriveType.Fixed || !drive.IsReady)
                {
                    continue;
                }

                string fileSystem = drive.DriveFormat;
                long totalSize = drive.TotalSize;
                string? source = mountSources?.GetValueOrDefault(drive.Name);
                if (!IsReportable(drive.Name, fileSystem, totalSize, source))
                {
                    continue;
                }

                ulong total = (ulong)totalSize;
                ulong free = (ulong)drive.AvailableFreeSpace;

                volumes.Add(new DiskVolumeInfo
                {
                    MountPoint = drive.Name,
                    FileSystem = fileSystem,
                    TotalBytes = total,
                    UsedBytes = total > free ? total - free : 0
                });
            }
            catch
            {
                // A drive can become unready/inaccessible between GetDrives()
                // returning it and reading its properties; skip and continue.
            }
        }

        return volumes;
    }

    // Kernel/virtual filesystems that DriveInfo on Unix reports as Fixed even
    // though they hold no user data (e.g. efivarfs, tracefs, rpc_pipefs), plus
    // read-only squashfs images (snap packages, live media).
    private static readonly HashSet<string> PseudoFileSystems = new(StringComparer.OrdinalIgnoreCase)
    {
        "autofs", "binfmt_misc", "bpf", "cgroup", "cgroup2", "configfs", "debugfs",
        "devpts", "devtmpfs", "efivarfs", "fusectl", "hugetlbfs", "mqueue", "nsfs",
        "proc", "pstore", "ramfs", "rpc_pipefs", "securityfs", "selinuxfs", "squashfs",
        "sysfs", "tracefs", "tmpfs", "devfs"
    };
}
