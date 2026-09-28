using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms;

[SupportedOSPlatform("freebsd")]
public sealed partial class FreeBSDHostProbe : IHostProbe
{
    private const int KenvGet = 0;

    private static readonly string[] GenericNames =
    [
        "system product name",
        "default string",
        "to be filled by o.e.m.",
        "none",
        "unknown"
    ];

    [LibraryImport("libc", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int sysctlbyname(
        string name,
        Span<byte> oldp,
        ref nuint oldlenp,
        IntPtr newp,
        nuint newlen);

    [LibraryImport("libc", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int kenv(int what, string name, Span<byte> value, int len);

    public HostInfo? Detect()
    {
        // 1. Bare metal: SMBIOS strings exposed via the kernel environment
        // (FreeBSD has no /sys/class/dmi/id equivalent - kenv is the real
        // source `dmidecode`/`kenv -q` themselves read from).
        string product = GetKenvString("smbios.system.product");
        string vendor = GetKenvString("smbios.system.maker");
        string version = GetKenvString("smbios.system.version");

        if (IsGeneric(product))
        {
            product = GetKenvString("smbios.planar.product");
        }

        if (IsGeneric(vendor))
        {
            vendor = GetKenvString("smbios.planar.maker");
        }

        if (!string.IsNullOrEmpty(product) && !IsGeneric(product))
        {
            string fullName;
            if (!string.IsNullOrEmpty(vendor) && !product.Contains(vendor, StringComparison.OrdinalIgnoreCase))
            {
                fullName = $"{vendor} {product}";
            }
            else
            {
                fullName = product;
            }

            return new HostInfo
            {
                Name = fullName,
                Vendor = string.IsNullOrEmpty(vendor) ? null : vendor,
                Version = string.IsNullOrEmpty(version) ? null : version
            };
        }

        // 2. Fall back to reporting the hypervisor when SMBIOS has nothing
        // usable (some VM configurations leave these fields blank).
        string guest = GetSysctlString("kern.vm_guest");
        if (!string.IsNullOrEmpty(guest) && !guest.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            return new HostInfo
            {
                Name = $"{guest.ToUpperInvariant()} Virtual Machine"
            };
        }

        return null;
    }

    private static bool IsGeneric(string val)
    {
        if (string.IsNullOrEmpty(val)) return true;
        string lower = val.ToLowerInvariant();
        return GenericNames.Any(g => lower.Contains(g));
    }

    private static string GetSysctlString(string name)
    {
        Span<byte> buffer = stackalloc byte[256];
        nuint len = (nuint)buffer.Length;
        if (sysctlbyname(name, buffer, ref len, IntPtr.Zero, 0) == 0 && len > 1)
        {
            return Encoding.UTF8.GetString(buffer[..(int)(len - 1)]);
        }
        return string.Empty;
    }

    private static string GetKenvString(string name)
    {
        try
        {
            Span<byte> buffer = stackalloc byte[256];
            int written = kenv(KenvGet, name, buffer, buffer.Length);
            if (written <= 0)
            {
                return string.Empty;
            }

            int len = Math.Min(written, buffer.Length);
            int nul = buffer[..len].IndexOf((byte)0);
            if (nul >= 0)
            {
                len = nul;
            }

            return Encoding.UTF8.GetString(buffer[..len]).Trim();
        }
        catch
        {
            // kenv unavailable (jail, restricted environment) - not a hard error.
            return string.Empty;
        }
    }
}
