using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using RackPeek.Domain.Resources.Services.Networking;

namespace RackPeek.Domain.Discovery;

/// <summary>The real network IO. Deliberately dumb; see <see cref="INetworkProbe" />.</summary>
public sealed class NetworkProbe : INetworkProbe {
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
        return await SystemProbeCommon.TryReadFileAsync("/proc/net/arp", cancellationToken)
               ?? await SystemProbeCommon.TryRunAsync("arp", "-an", cancellationToken);
    }

    public async Task<string?> ReverseDnsAsync(string ip, CancellationToken cancellationToken = default) {
        try {
            // A resolver with a dead PTR zone can sit on the query far longer than the
            // whole sweep took; the cap keeps a pile of dead lookups from stalling it.
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(2));

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

                UnicastIPAddressInformation? address = properties.UnicastAddresses.FirstOrDefault(a =>
                    a.Address.AddressFamily == AddressFamily.InterNetwork);

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
}
