using System.ComponentModel;
using RackPeek.Domain.Discovery;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Discovery;

public sealed class DiscoverDockerSettings : DiscoverSettings {
    [CommandOption("--docker-host <URI>")]
    [Description("Docker endpoint, e.g. unix:///var/run/docker.sock or tcp://host:2375. " +
                 "Defaults to DOCKER_HOST, then the local socket.")]
    public string? DockerHost { get; init; }

    [CommandOption("--host <NAME>")]
    [Description("Name of the machine these containers run on. Defaults to its hostname.")]
    public string? HostName { get; init; }
}

/// <summary>Reads the Docker Engine API and emits each published container as a Service.</summary>
public sealed class DiscoverDockerCommand(IEnumerable<ISystemProbe> probes)
    : AsyncCommand<DiscoverDockerSettings> {
    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        DiscoverDockerSettings settings,
        CancellationToken cancellationToken) {
        // The host's own facts give the services a stable id seed, the address they are
        // reachable on, and something to hang runsOn off.
        SystemFacts host = await ReadHostAsync(cancellationToken);

        DockerApiClient client;

        try {
            client = new DockerApiClient(settings.DockerHost);
        }
        catch (UriFormatException ex) {
            AnsiConsole.MarkupLine(
                $"[red]'{Markup.Escape(settings.DockerHost ?? string.Empty)}' is not a usable Docker endpoint.[/] " +
                $"{Markup.Escape(ex.Message)}");

            return 1;
        }

        using DockerApiClient _ = client;

        IReadOnlyList<DockerContainer> containers;

        try {
            containers = await client.ListContainersAsync(cancellationToken);
        }
        catch (Exception ex) when (
            ex is HttpRequestException or IOException or TimeoutException
            // HttpClient reports its own timeout as a cancellation.
            || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested)) {
            AnsiConsole.MarkupLine(
                $"[red]Could not reach Docker at {Markup.Escape(client.Endpoint)}.[/] " +
                $"{Markup.Escape(ex.Message)}");

            return 1;
        }

        // Over TCP the machine running this command is not the machine running the
        // containers, so the engine is asked about itself instead of trusting the local
        // probe: its daemon id seeds the services' identity (the same ids from any
        // workstation), and its hostname is what runsOn should point at.
        DockerEngineInfo? engine = client.IsLocal ? null : await client.GetInfoAsync(cancellationToken);

        if (!client.IsLocal && engine == null)
            AnsiConsole.MarkupLine(
                "[grey]The engine does not expose /info (a restricted socket proxy blocks it by " +
                "default), so the endpoint itself is the identity seed — keep addressing this " +
                "engine the same way, and pass --host to name the machine it runs on.[/]");

        // Named through the same mapper the system collector uses, so runsOn always
        // points at exactly the resource 'rpk discover system' produces on that machine.
        SystemResource hostResource = SystemResourceMapper.ToResource(host, settings.HostName);

        var hostName = client.IsLocal
            ? hostResource.Name
            : settings.HostName ?? engine?.Hostname ?? hostResource.Name;

        var seed = client.IsLocal
            ? host.MachineId ?? host.Hostname
            : engine?.Id ?? client.Endpoint;

        // Published ports live on the engine host, so a remote service's address is the
        // endpoint the user dialled — the local probe's address is only the last resort.
        var serviceIp = client.IsLocal
            ? host.Ip
            : await DockerApiClient.ResolveIpv4Async(client.RemoteHost!, cancellationToken) ?? host.Ip;

        List<Service> services = DockerServiceMapper.ToResources(containers, seed, hostName, serviceIp);

        var skipped = containers.Count - services.Count;

        if (skipped > 0)
            AnsiConsole.MarkupLine(
                $"[grey]Skipped {skipped} container(s) not reachable from outside the host.[/]");

        // The host System rides along so the server can line runsOn up by the host's id
        // even after the user has renamed it — a name alone could not be reconciled. Over
        // TCP the facts probed here describe this machine, not the engine's, so they stay
        // out; a rename there is preserved instead by the merge keeping the stored link
        // whenever an update's runsOn points at nothing.
        List<Resource> resources = client.IsLocal && services.Count > 0
            ? [hostResource, .. services]
            : [.. services];

        return await DiscoveryOutput.EmitAsync(resources, settings, cancellationToken);
    }

    private async Task<SystemFacts> ReadHostAsync(CancellationToken cancellationToken) {
        // Unlike `discover system`, an unsupported platform is not fatal here — the
        // containers can still be read; only the host's own facts fall back to basics.
        return await SystemProbes.TryReadHostAsync(probes, cancellationToken)
               ?? SystemFactsParser.Parse(new RawSystemSnapshot {
                   Hostname = Environment.MachineName,
                   Cores = Environment.ProcessorCount
               });
    }
}
