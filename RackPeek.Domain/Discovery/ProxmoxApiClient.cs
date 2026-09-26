using System.Net.Security;

namespace RackPeek.Domain.Discovery;

/// <summary>
///     Talks to the Proxmox VE API with an API token. A token is used rather than a
///     password because it can be given a read-only role and revoked on its own.
/// </summary>
public sealed class ProxmoxApiClient : IProxmoxClient, IDisposable {
    public const string QemuEndpoint = "qemu";
    public const string LxcEndpoint = "lxc";
    public const string TokenIdEnvironmentVariable = "RPK_PVE_TOKEN_ID";
    public const string TokenSecretEnvironmentVariable = "RPK_PVE_TOKEN_SECRET";

    private readonly HttpClient _httpClient;

    /// <param name="allowUntrustedCertificate">
    ///     Proxmox ships with a self-signed certificate and most installations keep it,
    ///     so this is needed more often than not. It is opt-in all the same.
    /// </param>
    public ProxmoxApiClient(
        string host,
        string tokenId,
        string tokenSecret,
        bool allowUntrustedCertificate = false,
        HttpClient? httpClient = null) {
        Endpoint = Normalise(host);

        _httpClient = httpClient ?? new HttpClient(Handler(allowUntrustedCertificate));
        _httpClient.BaseAddress = new Uri(Endpoint + "/api2/json/");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);

        // Proxmox expects the whole token as one opaque Authorization value.
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
            "Authorization",
            $"PVEAPIToken={tokenId}={tokenSecret}");
    }

    public string Endpoint { get; }

    public void Dispose() => _httpClient.Dispose();

    public async Task<string> GetIdentityScopeAsync(CancellationToken cancellationToken = default) {
        // A standalone host has no cluster, and Proxmox answers 5xx rather than an empty
        // list, so a failure here is expected and means "not clustered".
        try {
            var json = await GetAsync("cluster/status", cancellationToken);
            var clusterName = ProxmoxResponseParser.ParseIdentityScope(json, string.Empty);

            // Only fall back to the node list when there is no cluster name — the
            // fallback costs a second call, and on a cluster it would be thrown away.
            return clusterName.Length > 0 ? clusterName : await FirstNodeAsync(cancellationToken);
        }
        catch (HttpRequestException) {
            // Either there is no cluster, or the token may not read it. Either way the
            // node is a sound scope: a vmid is unique within it.
            return await FirstNodeAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<ProxmoxNode>> GetNodesAsync(CancellationToken cancellationToken = default) =>
        ProxmoxResponseParser.ParseNodes(await GetAsync("nodes", cancellationToken));

    public async Task<ProxmoxNode> EnrichAsync(
        ProxmoxNode node,
        CancellationToken cancellationToken = default) {
        try {
            // The three endpoints are independent, so the round trips overlap.
            Task<string> status = GetAsync($"nodes/{Uri.EscapeDataString(node.Name)}/status", cancellationToken);
            Task<IReadOnlyList<ProxmoxDisk>> disks = GetDisksAsync(node.Name, cancellationToken);
            Task<IReadOnlyList<ProxmoxGpu>> gpus = GetGpusAsync(node.Name, cancellationToken);

            ProxmoxNode detail = ProxmoxResponseParser.ParseNodeStatus(await status, node.Name);

            return node with {
                Cores = detail.Cores > 0 ? detail.Cores : node.Cores,
                MemoryBytes = detail.MemoryBytes > 0 ? detail.MemoryBytes : node.MemoryBytes,
                Version = detail.Version,
                CpuModel = detail.CpuModel,
                Sockets = detail.Sockets,
                PhysicalCores = detail.PhysicalCores,
                Disks = await disks,
                Gpus = await gpus
            };
        }
        catch (HttpRequestException) {
            return node;
        }
    }

    public async Task<IReadOnlyList<ProxmoxGuest>> GetGuestsAsync(
        string node,
        string endpoint,
        CancellationToken cancellationToken = default) {
        var json = await GetAsync($"nodes/{Uri.EscapeDataString(node)}/{endpoint}", cancellationToken);

        return ProxmoxResponseParser.ParseGuests(
            json,
            node,
            endpoint == LxcEndpoint ? ProxmoxResponseParser.ContainerType : ProxmoxResponseParser.VmType);
    }

    public async Task<IReadOnlyList<ProxmoxDisk>> GetDisksAsync(
        string node,
        CancellationToken cancellationToken = default) {
        try {
            return ProxmoxResponseParser.ParseDisks(
                await GetAsync($"nodes/{Uri.EscapeDataString(node)}/disks/list", cancellationToken));
        }
        catch (HttpRequestException) {
            return [];
        }
    }

    public async Task<IReadOnlyList<ProxmoxGpu>> GetGpusAsync(
        string node,
        CancellationToken cancellationToken = default) {
        try {
            return ProxmoxResponseParser.ParseGpus(
                await GetAsync($"nodes/{Uri.EscapeDataString(node)}/hardware/pci", cancellationToken));
        }
        catch (HttpRequestException) {
            return [];
        }
    }

    public async Task<ProxmoxGuestConfig> GetGuestConfigAsync(
        string node,
        string endpoint,
        int vmId,
        CancellationToken cancellationToken = default) {
        // A guest can disappear between listing and reading it; that is not worth failing
        // the whole run over, so it simply contributes nothing.
        try {
            var json = await GetAsync(
                $"nodes/{Uri.EscapeDataString(node)}/{endpoint}/{vmId}/config",
                cancellationToken);

            return ProxmoxResponseParser.ParseGuestConfig(json);
        }
        catch (HttpRequestException) {
            return new ProxmoxGuestConfig(null, null, [], []);
        }
    }

    private async Task<string> FirstNodeAsync(CancellationToken cancellationToken) {
        IReadOnlyList<ProxmoxNode> nodes = await GetNodesAsync(cancellationToken);

        return nodes.FirstOrDefault()?.Name ?? "proxmox";
    }

    private async Task<string> GetAsync(string path, CancellationToken cancellationToken) {
        using HttpResponseMessage response = await _httpClient.GetAsync(path, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new HttpRequestException(
                "Proxmox rejected the API token (401). Check the token id is of the form " +
                "user@realm!tokenname and that the secret matches.");

        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new HttpRequestException(
                $"The API token is not permitted to read {path} (403). In the Proxmox UI: " +
                "Datacenter -> Permissions -> Add -> API Token Permission, path '/', " +
                "role PVEAuditor, with Propagate ticked.");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static HttpClientHandler Handler(bool allowUntrustedCertificate) {
        var handler = new HttpClientHandler();

        if (allowUntrustedCertificate)
            handler.ServerCertificateCustomValidationCallback =
                (_, _, _, _) => true;

        return handler;
    }

    private static string Normalise(string host) {
        var trimmed = host.Trim().TrimEnd('/');

        if (trimmed.Contains("://", StringComparison.Ordinal))
            return trimmed;

        // A bare name gets the default scheme and port, but "pve.lan:8006" already
        // carries a port — appending another would make the URL unparseable.
        return trimmed.Contains(':', StringComparison.Ordinal)
            ? $"https://{trimmed}"
            : $"https://{trimmed}:8006";
    }
}
