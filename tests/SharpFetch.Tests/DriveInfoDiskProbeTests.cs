using SharpFetch.Core.Platforms.Common;
using Xunit;

namespace SharpFetch.Tests;

public class DriveInfoDiskProbeTests
{
    private const long FiftyGiB = 50L * 1024 * 1024 * 1024;

    [Theory]
    [InlineData("/", "xfs")]
    [InlineData("/home", "ext4")]
    [InlineData("C:\\", "NTFS")]
    [InlineData("/", "apfs")]
    [InlineData("/mnt/windows", "fuseblk")]
    public void IsReportable_RealFileSystemWithSize_ReturnsTrue(string mountPoint, string fileSystem)
    {
        Assert.True(DriveInfoDiskProbe.IsReportable(mountPoint, fileSystem, FiftyGiB));
    }

    [Theory]
    [InlineData("efivarfs")]
    [InlineData("tracefs")]
    [InlineData("rpc_pipefs")]
    [InlineData("SYSFS")]
    [InlineData("squashfs")]
    [InlineData("fuse.portal")]
    [InlineData("fuse.gvfsd-fuse")]
    public void IsReportable_PseudoFileSystem_ReturnsFalse(string fileSystem)
    {
        Assert.False(DriveInfoDiskProbe.IsReportable("/some/mount", fileSystem, FiftyGiB));
    }

    [Fact]
    public void IsReportable_ZeroSizeVolume_ReturnsFalse()
    {
        Assert.False(DriveInfoDiskProbe.IsReportable("/", "xfs", 0));
    }

    [Theory]
    [InlineData("/System/Volumes/VM")]
    [InlineData("/System/Volumes/Preboot")]
    [InlineData("/System/Volumes/Data")]
    public void IsReportable_MacOsSystemVolume_ReturnsFalse(string mountPoint)
    {
        Assert.False(DriveInfoDiskProbe.IsReportable(mountPoint, "apfs", FiftyGiB));
    }

    [Fact]
    public void IsReportable_MountWithoutDeviceSource_ReturnsFalse()
    {
        Assert.False(DriveInfoDiskProbe.IsReportable("/var/lib/docker/overlay2/x/merged", "overlay", FiftyGiB, "overlay"));
    }

    [Fact]
    public void IsReportable_RootOnOverlay_ReturnsTrue()
    {
        // Containers mount "/" from "overlay"; the root must still be shown.
        Assert.True(DriveInfoDiskProbe.IsReportable("/", "overlay", FiftyGiB, "overlay"));
    }

    [Fact]
    public void IsReportable_MountBackedByDevice_ReturnsTrue()
    {
        Assert.True(DriveInfoDiskProbe.IsReportable("/data", "ext4", FiftyGiB, "/dev/nvme1n1p1"));
    }

    [Fact]
    public void ParseMountSources_MapsMountPointsAndUnescapesSpaces()
    {
        var sources = DriveInfoDiskProbe.ParseMountSources(
        [
            "/dev/nvme0n1p1 / xfs rw,noatime 0 0",
            "sysfs /sys sysfs rw,nosuid 0 0",
            "/dev/sdb1 /media/My\\040Disk vfat rw 0 0",
            "tmpfs /mnt tmpfs rw 0 0",
            "/dev/sdc1 /mnt ext4 rw 0 0",
            "malformed",
        ]);

        Assert.Equal("/dev/nvme0n1p1", sources["/"]);
        Assert.Equal("sysfs", sources["/sys"]);
        Assert.Equal("/dev/sdb1", sources["/media/My Disk"]);
        Assert.Equal("/dev/sdc1", sources["/mnt"]); // last mount on top wins
        Assert.Equal(4, sources.Count);
    }
}
