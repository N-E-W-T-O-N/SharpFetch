using System.Diagnostics;

namespace SharpFetch.Core.Platforms.Common;

/// <summary>
/// Runs a short-lived helper command (system_profiler, pmset) and returns its
/// stdout, or null if it could not start, failed, or overran its timeout.
/// Calling StandardOutput.ReadToEnd() before WaitForExit(timeout) blocks until
/// the child exits, so the timeout would never apply. Reading asynchronously
/// lets a hung child be killed instead of hanging the whole fetch.
/// </summary>
internal static class ProcessRunner
{
    public static string? TryRun(string fileName, string arguments, int timeoutMs)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                // Not redirecting stderr: redirecting a stream nobody drains
                // risks a deadlock if the child fills that pipe's OS buffer.
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process is null)
            {
                return null;
            }

            Task<string> output = process.StandardOutput.ReadToEndAsync();

            // One deadline covers both the exit and the stdout drain, so a
            // descendant holding the pipe open can't stretch it to 2x timeout.
            if (process.WaitForExit(timeoutMs) &&
                output.Wait(Math.Max(0, timeoutMs - (int)stopwatch.ElapsedMilliseconds)))
            {
                return output.Result;
            }

            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Already exited between the timeout and the kill.
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
