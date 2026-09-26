namespace RackPeek.Domain.Discovery;

/// <summary>Reads containers off a Docker Engine API. The IO half of docker discovery.</summary>
public interface IDockerClient {
    /// <summary>Describes where this client is pointed, for error messages.</summary>
    string Endpoint { get; }

    /// <summary>
    ///     Running containers only. A stopped container has no host port bindings — the
    ///     daemon does not create them until it runs — so it can never be described as a
    ///     Service, which makes listing stopped containers pointless here.
    /// </summary>
    Task<IReadOnlyList<DockerContainer>> ListContainersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     The engine's own identity, or null when it cannot be read. Null is an expected
    ///     answer, not an error: the read-only socket proxies the docs recommend usually
    ///     allow <c>/containers</c> but block <c>/info</c>.
    /// </summary>
    Task<DockerEngineInfo?> GetInfoAsync(CancellationToken cancellationToken = default);
}
