using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace RackPeek.Domain.Discovery;

/// <summary>Host reads that the BCL already does the same way on every platform.</summary>
internal static class SystemProbeCommon {
    public static string Hostname() {
        try {
            return Dns.GetHostName();
        }
        catch {
            return Environment.MachineName;
        }
    }

    public static int Cores() => Environment.ProcessorCount;

    public static long FallbackMemoryBytes() => GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;

    public static IReadOnlyList<NicFact> Nics() {
        try {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Select(ToFact)
                .Where(n => n.Ipv4 != null)
                .ToList();
        }
        catch {
            return [];
        }
    }

    private static NicFact ToFact(NetworkInterface nic) {
        IPInterfaceProperties properties = nic.GetIPProperties();

        var ipv4 = properties.UnicastAddresses
            .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
            ?.Address.ToString();

        var hasGateway = properties.GatewayAddresses
            .Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork
                      && !g.Address.Equals(IPAddress.Any));

        return new NicFact(
            nic.Name,
            nic.OperationalStatus == OperationalStatus.Up,
            nic.NetworkInterfaceType == NetworkInterfaceType.Loopback,
            hasGateway,
            ipv4);
    }

    /// <summary>Reads a file, returning null for anything unreadable rather than throwing.</summary>
    public static async Task<string?> TryReadFileAsync(string path, CancellationToken cancellationToken) {
        try {
            return File.Exists(path)
                ? await File.ReadAllTextAsync(path, cancellationToken)
                : null;
        }
        catch {
            return null;
        }
    }

    /// <summary>
    ///     Runs a command and returns stdout, or null if it cannot be run, fails, or
    ///     takes longer than a few seconds — a hung probe must not hang a timer-driven
    ///     <c>rpk discover</c> forever. Stderr is drained concurrently so a chatty child
    ///     cannot deadlock on a full pipe.
    /// </summary>
    public static async Task<string?> TryRunAsync(
        string fileName,
        string arguments,
        CancellationToken cancellationToken) {
        try {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));

            using var process = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            if (!process.Start())
                return null;

            try {
                Task<string> stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
                Task<string> stderr = process.StandardError.ReadToEndAsync(timeout.Token);

                await process.WaitForExitAsync(timeout.Token);
                await stderr;

                return process.ExitCode == 0 ? (await stdout).Trim() : null;
            }
            catch (OperationCanceledException) {
                try {
                    process.Kill(true);
                }
                catch {
                    // It may have exited in the meantime; nothing left to do.
                }

                return null;
            }
        }
        catch {
            return null;
        }
    }
}
