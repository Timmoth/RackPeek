using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using RackPeek.Domain.Resources.Services.Networking;

namespace RackPeek.Domain.Discovery;

/// <summary>The real network IO. Deliberately dumb; see <see cref="INetworkProbe" />.</summary>
public sealed class NetworkProbe : INetworkProbe {
    public bool IsSupported => !OperatingSystem.IsBrowser();

    public async Task<bool> PingAsync(string ip, TimeSpan timeout, CancellationToken cancellationToken = default) {
        try {
            using var ping = new Ping();
            PingReply reply = await ping.SendPingAsync(ip, timeout, cancellationToken: cancellationToken);

            return reply.Status == IPStatus.Success;
        }
        catch {
            // No ICMP privilege, unreachable network, bad address — all mean "no answer".
            return false;
        }
    }

    public async Task<bool> TryConnectAsync(
        string ip,
        int port,
        TimeSpan timeout,
        CancellationToken cancellationToken = default) {
        try {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            await socket.ConnectAsync(IPAddress.Parse(ip), port, cts.Token);

            return true;
        }
        catch {
            // Refused, timed out, filtered — for liveness they are all the same "no".
            return false;
        }
    }

    public async Task<string?> ReadArpAsync(CancellationToken cancellationToken = default) {
        // Linux reads the kernel's file; BSD/macOS answer `arp -an`; Windows' arp.exe
        // only knows `-a`. A source only wins if it yields entries the parser can use —
        // an exit code alone is not proof (arp.exe printing usage text could exit 0),
        // and trusting one would silently cost every host its MAC identity.
        foreach (Func<Task<string?>> read in new Func<Task<string?>>[] {
                     () => SystemProbeCommon.TryReadFileAsync("/proc/net/arp", cancellationToken),
                     () => SystemProbeCommon.TryRunAsync("arp", "-an", cancellationToken),
                     () => SystemProbeCommon.TryRunAsync("arp", "-a", cancellationToken)
                 }) {
            var text = await read();

            if (text != null && ArpTableParser.Parse(text).Count > 0)
                return text;
        }

        return null;
    }

    public async Task<string?> ReverseDnsAsync(
        string ip,
        TimeSpan timeout,
        CancellationToken cancellationToken = default) {
        try {
            // A resolver with a dead PTR zone can sit on the query far longer than the
            // whole sweep took; the cap keeps a pile of dead lookups from stalling it.
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            IPHostEntry entry = await Dns.GetHostEntryAsync(ip, cts.Token);

            // Some resolvers answer a PTR miss by echoing the address back.
            return string.IsNullOrWhiteSpace(entry.HostName) || entry.HostName == ip
                ? null
                : entry.HostName;
        }
        catch {
            return null;
        }
    }

    public async Task<string?> ReadTlsSubjectAsync(
        string ip,
        int port,
        TimeSpan timeout,
        CancellationToken cancellationToken = default) {
        try {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Parse(ip), port, cts.Token);

            // Every certificate is accepted: homelab gear is self-signed by default and
            // the certificate is being read for its name, never trusted for security.
            // The callback goes in the options only — setting it in the constructor too
            // makes AuthenticateAsClientAsync throw.
            await using var ssl = new SslStream(client.GetStream(), false);

            await ssl.AuthenticateAsClientAsync(
                new SslClientAuthenticationOptions {
                    TargetHost = ip,
                    RemoteCertificateValidationCallback = (_, _, _, _) => true
                },
                cts.Token);

            return ssl.RemoteCertificate is { } certificate
                ? new X509Certificate2(certificate).Subject
                : null;
        }
        catch {
            // Closed, plaintext, or a handshake this runtime will not do — all "no name".
            return null;
        }
    }

    public async Task<string?> ReadTcpBannerAsync(
        string ip,
        int port,
        TimeSpan timeout,
        CancellationToken cancellationToken = default) {
        try {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Parse(ip), port, cts.Token);

            await using NetworkStream stream = client.GetStream();

            var buffer = new byte[256];
            var read = await stream.ReadAsync(buffer, cts.Token);

            return read > 0
                ? Encoding.ASCII.GetString(buffer, 0, read).Trim()
                : null;
        }
        catch {
            return null;
        }
    }

    public async Task<string?> ReadHttpHeadAsync(
        string ip,
        int port,
        bool tls,
        TimeSpan timeout,
        CancellationToken cancellationToken = default) {
        try {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Parse(ip), port, cts.Token);

            Stream stream = client.GetStream();
            SslStream? ssl = null;

            if (tls) {
                ssl = new SslStream(stream, false);

                await ssl.AuthenticateAsClientAsync(
                    new SslClientAuthenticationOptions {
                        TargetHost = ip,
                        RemoteCertificateValidationCallback = (_, _, _, _) => true
                    },
                    cts.Token);

                stream = ssl;
            }

            try {
                // HTTP/1.0 so the server closes the connection itself rather than leaving
                // the read waiting on a keep-alive timeout.
                var request = Encoding.ASCII.GetBytes(
                    $"GET / HTTP/1.0\r\nHost: {ip}\r\nUser-Agent: rackpeek-discover\r\nAccept: */*\r\nConnection: close\r\n\r\n");

                await stream.WriteAsync(request, cts.Token);
                await stream.FlushAsync(cts.Token);

                // Enough for the headers and a <title> near the top of the body. Capped so
                // a host streaming megabytes cannot hold the sweep open.
                var buffer = new byte[8192];
                var total = 0;

                while (total < buffer.Length) {
                    var read = await stream.ReadAsync(buffer.AsMemory(total), cts.Token);

                    if (read == 0)
                        break;

                    total += read;
                }

                return total > 0
                    ? Encoding.UTF8.GetString(buffer, 0, total)
                    : null;
            }
            finally {
                if (ssl != null)
                    await ssl.DisposeAsync();
                else
                    await stream.DisposeAsync();
            }
        }
        catch {
            return null;
        }
    }

    public Cidr? LocalSubnet() {
        try {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces()) {
                if (nic.OperationalStatus != OperationalStatus.Up
                    || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                IPInterfaceProperties properties = nic.GetIPProperties();

                var hasGateway = properties.GatewayAddresses.Any(g =>
                    g.Address.AddressFamily == AddressFamily.InterNetwork
                    && !g.Address.Equals(IPAddress.Any));

                if (!hasGateway)
                    continue;

                // Skip 169.254/16 self-assigned addresses: a NIC mid-DHCP-renewal can
                // carry one alongside its real address, and sweeping that block finds
                // nothing by definition.
                UnicastIPAddressInformation? address = properties.UnicastAddresses.FirstOrDefault(a =>
                    a.Address.AddressFamily == AddressFamily.InterNetwork
                    && !IsLinkLocal(a.Address));

                if (address == null)
                    continue;

                return Cidr.Parse($"{address.Address}/{address.PrefixLength}");
            }
        }
        catch {
            // Fall through: the caller asks the user for --cidr instead.
        }

        return null;
    }

    private static bool IsLinkLocal(IPAddress address) {
        var bytes = address.GetAddressBytes();

        return bytes.Length == 4 && bytes[0] == 169 && bytes[1] == 254;
    }
}
