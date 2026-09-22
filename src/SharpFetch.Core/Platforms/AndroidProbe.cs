using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using SharpFetch.Core.Models;
using SharpFetch.Core.Probes;

namespace SharpFetch.Core.Platforms;

[SupportedOSPlatform("android")]
public sealed partial class AndroidProbe : IOsProbe
{
    /// <summary>
    /// PROP_VALUE_MAX from Android's &lt;sys/system_properties.h&gt;. The bionic
    /// implementation requires the caller's buffer to be at least this large.
    /// </summary>
    private const int PropValueMax = 92;

    [LibraryImport("libc", EntryPoint = "__system_property_get", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int SystemPropertyGet(string name, Span<byte> value);

    private static string GetSystemProperty(string name)
    {
        Span<byte> buffer = stackalloc byte[PropValueMax];
        int written = SystemPropertyGet(name, buffer);
        // Returns the value length excluding the null terminator, 0 when unset.
        return written > 0 && written <= buffer.Length
            ? Encoding.UTF8.GetString(buffer[..written])
            : string.Empty;
    }

    public OsInfo Detect()
    {
        string release = GetSystemProperty("ro.build.version.release");
        string sdk = GetSystemProperty("ro.build.version.sdk");

        string build = GetSystemProperty("ro.build.display.id");
        if (string.IsNullOrEmpty(build))
        {
            build = GetSystemProperty("ro.build.id");
        }
        if (string.IsNullOrEmpty(build) && !string.IsNullOrEmpty(sdk))
        {
            build = $"API {sdk}";
        }

        // Android exposes the standard Linux procfs, so the kernel release is
        // read the same way as on desktop Linux.
        string kernel = ReadFirstLine("/proc/sys/kernel/osrelease");
        if (string.IsNullOrEmpty(kernel))
        {
            kernel = Environment.OSVersion.Version.ToString();
        }

        string arch = RuntimeInformation.OSArchitecture switch
        {
            Architecture.Arm64 => "aarch64",
            Architecture.X64 => "x86_64",
            Architecture.Arm => "armv7l",
            Architecture.X86 => "i686",
            _ => RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant()
        };

        // Environment.MachineName is typically "localhost" on Android; the device
        // model is the identifier a user actually recognizes.
        string hostname = Environment.MachineName;
        if (string.IsNullOrEmpty(hostname) || hostname.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            string model = GetSystemProperty("ro.product.model");
            if (!string.IsNullOrEmpty(model))
            {
                hostname = model;
            }
        }

        return new OsInfo
        {
            Name = "Android",
            Version = release,
            Build = build,
            Kernel = kernel,
            Architecture = arch,
            Family = OsFamily.Android,
            Codename = GetAndroidCodeName(sdk),
            Hostname = hostname,
            Username = Environment.UserName,
            Uptime = GetUptime()
        };
    }

    /// <summary>
    /// Maps an Android API level to its internal dessert codename. Covers API
    /// 21-35; newer levels return null until their codename is confirmed rather
    /// than reporting a guess.
    /// </summary>
    private static string? GetAndroidCodeName(string sdk)
    {
        if (!int.TryParse(sdk, NumberStyles.Integer, CultureInfo.InvariantCulture, out int apiLevel))
        {
            return null;
        }

        return apiLevel switch
        {
            35 => "Vanilla Ice Cream",
            34 => "Upside Down Cake",
            33 => "Tiramisu",
            31 or 32 => "Snow Cone",
            30 => "Red Velvet Cake",
            29 => "Quince Tart",
            28 => "Pie",
            26 or 27 => "Oreo",
            24 or 25 => "Nougat",
            23 => "Marshmallow",
            21 or 22 => "Lollipop",
            _ => null
        };
    }

    private static TimeSpan GetUptime()
    {
        try
        {
            string text = ReadFirstLine("/proc/uptime");
            if (text.Length > 0)
            {
                string firstNum = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
                if (double.TryParse(firstNum, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds))
                {
                    return TimeSpan.FromSeconds(seconds);
                }
            }
        }
        catch
        {
            // Fall through to the BCL tick count
        }

        return TimeSpan.FromMilliseconds(Environment.TickCount64);
    }

    private static string ReadFirstLine(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path).Trim() : string.Empty;
        }
        catch
        {
            // procfs entries can be restricted by SELinux policy on some devices
            return string.Empty;
        }
    }
}
