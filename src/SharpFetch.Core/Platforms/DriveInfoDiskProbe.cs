using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms;

/// <summary>
/// System.IO.DriveInfo is genuinely cross-platform BCL - unlike CPU/GPU/OS
/// detection, disk usage needs no P/Invoke or per-OS code, so there is only
/// one implementation of this probe rather than one per platform.
/// </summary>
public sealed class DriveInfoDiskProbe : IDiskProbe
{
    public IReadOnlyList<DiskVolumeInfo> Detect()
    {
        var volumes = new List<DiskVolumeInfo>();

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

                ulong total = (ulong)drive.TotalSize;
                ulong free = (ulong)drive.AvailableFreeSpace;

                volumes.Add(new DiskVolumeInfo
                {
                    MountPoint = drive.Name,
                    FileSystem = drive.DriveFormat,
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
}
