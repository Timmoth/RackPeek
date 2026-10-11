using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using RackPeek.Domain.Api;
using RackPeek.Domain.Discovery;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Mcp.Tools;

public sealed record DiscoveryResult(
    [property: Description("What was found, as a RackPeek YAML document ready for upsert_resources.")]
    string Yaml,
    int ResourceCount,
    [property: Description("Containers that were skipped because they publish no port reachable from outside the host.")]
    int Skipped,
    [property: Description("The merge outcome when apply was true; null when only previewing.")]
    ImportYamlResponse? Applied);

/// <summary>
///     Discovery run from the server against reachable infrastructure. Credentials
///     are read from the server's own configuration, never from tool parameters, so
///     secrets stay out of the conversation. `rpk discover system` has no tool here
///     on purpose: it probes the machine it runs on, which for the server is its own
///     container — run the CLI on the machine being inventoried instead.
/// </summary>
[McpServerToolType]
public sealed class DiscoveryTools(IServiceProvider services) {
    [McpServerTool(Name = "discover_docker", UseStructuredContent = true, OpenWorld = true)]
    [Description("Reads a Docker (or Podman) engine and maps each container with a published port to a Service. " +
                 "Preview first (apply=false), then apply to merge into the inventory — merging never removes anything.")]
    public Task<DiscoveryResult> DiscoverDocker(
        [Description("Docker endpoint, e.g. tcp://host:2375 or unix:///var/run/docker.sock. Defaults to DOCKER_HOST, then the local socket.")]
        string? dockerHost = null,
        [Description("Name of the machine the containers run on. Defaults to the engine's hostname.")]
        string? hostName = null,
        [Description("Merge the result into the inventory instead of only returning it.")]
        bool apply = false,
        CancellationToken cancellationToken = default) {
        return ToolErrors.RunAsync(async () => {
            SystemFacts host = await ReadHostAsync(cancellationToken);

            DockerApiClient client;
            try {
                client = new DockerApiClient(dockerHost);
            }
            catch (UriFormatException ex) {
                throw new McpException($"'{dockerHost}' is not a usable Docker endpoint. {ex.Message}");
            }

            using DockerApiClient _ = client;

            IReadOnlyList<DockerContainer> containers;
            try {
                containers = await client.ListContainersAsync(cancellationToken);
            }
            catch (Exception ex) when (
                ex is HttpRequestException or IOException or TimeoutException
                || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested)) {
                throw new McpException($"Could not reach Docker at {client.Endpoint}. {ex.Message}");
            }

            // Mirrors `rpk discover docker`: over TCP the engine describes itself (its
            // daemon id seeds identity, its hostname is what runsOn points at); locally
            // the host probe does, and the host System rides along for id-based merging.
            DockerEngineInfo? engine = client.IsLocal ? null : await client.GetInfoAsync(cancellationToken);

            SystemResource hostResource = SystemResourceMapper.ToResource(host, hostName);

            var effectiveHost = client.IsLocal
                ? hostResource.Name
                : hostName ?? engine?.Hostname ?? hostResource.Name;

            var seed = client.IsLocal
                ? host.MachineId ?? host.Hostname
                : engine?.Id ?? client.Endpoint;

            // No fallback to this machine's address: it is not the remote engine's, and
            // stamping it on would give every service a confidently wrong one.
            var serviceIp = client.IsLocal
                ? host.Ip
                : await DockerApiClient.ResolveIpv4Async(client.RemoteHost!, cancellationToken);

            if (string.IsNullOrWhiteSpace(serviceIp))
                throw new ValidationException(
                    $"Could not determine an IPv4 address for {client.Endpoint}. Services are "
                    + "recorded at their host's address and the inventory holds IPv4 only; "
                    + "dial the engine by address instead, e.g. tcp://192.0.2.10:2375.");

            List<Service> found = DockerServiceMapper.ToResources(containers, seed, effectiveHost, serviceIp);

            List<Resource> resources = client.IsLocal && found.Count > 0
                ? [hostResource, .. found]
                : [.. found];

            return await EmitAsync(resources, containers.Count - found.Count, apply);
        });
    }

    [McpServerTool(Name = "discover_proxmox", UseStructuredContent = true, OpenWorld = true)]
    [Description("Reads a Proxmox VE estate: each node becomes a Server plus a hypervisor System, each VM/LXC a System " +
                 "running on it, already wired together. Credentials come from the server's RPK_PVE_TOKEN_ID / " +
                 "RPK_PVE_TOKEN_SECRET configuration. Preview first (apply=false), then apply to merge.")]
    public Task<DiscoveryResult> DiscoverProxmox(
        [Description("Proxmox host, e.g. https://pve.lan:8006. A bare host name gets https and :8006.")]
        string host,
        [Description("Accept a self-signed certificate, which Proxmox ships with by default.")]
        bool insecure = false,
        [Description("Merge the result into the inventory instead of only returning it.")]
        bool apply = false,
        CancellationToken cancellationToken = default) {
        return ToolErrors.RunAsync(async () => {
            IConfiguration config = services.GetRequiredService<IConfiguration>();
            var tokenId = config[ProxmoxApiClient.TokenIdEnvironmentVariable];
            var tokenSecret = config[ProxmoxApiClient.TokenSecretEnvironmentVariable];

            if (string.IsNullOrWhiteSpace(tokenId) || string.IsNullOrWhiteSpace(tokenSecret))
                throw new McpException(
                    "Proxmox credentials are not configured on the server. Start it with " +
                    $"{ProxmoxApiClient.TokenIdEnvironmentVariable} and {ProxmoxApiClient.TokenSecretEnvironmentVariable} set.");

            ProxmoxApiClient client;
            try {
                client = new ProxmoxApiClient(host, tokenId, tokenSecret, insecure);
            }
            catch (UriFormatException ex) {
                throw new McpException($"'{host}' is not a usable host. {ex.Message}");
            }

            List<Resource> resources;
            try {
                resources = await ProxmoxDiscovery.ReadAsync(client, cancellationToken);
            }
            catch (HttpRequestException ex) {
                var hint = !insecure && ex.InnerException is System.Security.Authentication.AuthenticationException
                    ? " Proxmox uses a self-signed certificate by default — try insecure=true."
                    : string.Empty;

                throw new McpException($"Could not read {client.Endpoint}. {ex.Message}{hint}");
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) {
                // HttpClient reports its timeout as a cancellation.
                throw new McpException($"{client.Endpoint} did not answer within the timeout.");
            }
            finally {
                client.Dispose();
            }

            return await EmitAsync(resources, 0, apply);
        });
    }

    private async Task<SystemFacts> ReadHostAsync(CancellationToken cancellationToken) {
        // An unsupported platform is not fatal for docker discovery — the containers can
        // still be read; only the host's own facts fall back to basics.
        return await SystemProbes.TryReadHostAsync(services.GetServices<ISystemProbe>(), cancellationToken)
               ?? SystemFactsParser.Parse(new RawSystemSnapshot {
                   Hostname = Environment.MachineName,
                   Cores = Environment.ProcessorCount
               });
    }

    private async Task<DiscoveryResult> EmitAsync(List<Resource> resources, int skipped, bool apply) {
        var yaml = DiscoveryDocument.ToYaml(resources);

        ImportYamlResponse? applied = null;
        if (apply && resources.Count > 0)
            applied = await MutationTools.RunUpsertAsync(services, new ImportYamlRequest {
                Yaml = yaml,
                // Discovery can add and update but must never remove what the user wrote.
                Mode = MergeMode.Merge
            });

        return new DiscoveryResult(yaml, resources.Count, skipped, applied);
    }
}
