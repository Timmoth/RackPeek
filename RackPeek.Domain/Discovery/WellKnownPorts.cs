namespace RackPeek.Domain.Discovery;

/// <summary>
///     The TCP ports the sweep knocks on when a host ignores ping. Chosen for what a
///     homelab actually runs — one open port anywhere in this list is enough to call
///     the host alive, so breadth matters more than depth.
/// </summary>
public static class WellKnownPorts {
    public static readonly IReadOnlyList<int> Defaults = [
        22, // ssh — almost everything
        80, // http
        443, // https
        53, // dns — pi-hole, routers
        445, // smb — nas boxes
        3389, // rdp — windows
        631, // ipp — printers
        8006, // proxmox
        5000, // synology / registries
        8080, // alt http
        8443, // alt https
        9100 // node-exporter / jetdirect
    ];

    /// <summary>
    ///     The wider list a host is checked against once it has already proven it is
    ///     alive. Liveness is paid per address, so <see cref="Defaults" /> stays short;
    ///     this runs only on hosts that answered, where a dozen more connections cost
    ///     nothing and buy two things — a service on one of these ports is a strong
    ///     statement about what the machine is, and it is a target for the banner probes
    ///     that actually name it.
    /// </summary>
    public static readonly IReadOnlyList<int> Identity = [
        .. Defaults,
        554, // rtsp — cameras
        1883, // mqtt — home automation brokers
        3000, // grafana / forgejo / many node apps
        3306, // mysql
        5432, // postgres
        5900, // vnc
        6379, // redis
        7860, // gradio / stable-diffusion
        8000, // alt http
        8096, // jellyfin
        8123, // home assistant
        9000, // portainer / minio
        9090, // prometheus / cockpit
        11434, // ollama
        32400 // plex
    ];

    /// <summary>
    ///     What a port conventionally carries, for naming the service found on it. A port
    ///     number is a convention rather than a guarantee, so this only ever supplies a
    ///     name — anything a service actually said about itself wins over it.
    /// </summary>
    private static readonly Dictionary<int, string> _names = new() {
        [21] = "ftp",
        [22] = "ssh",
        [23] = "telnet",
        [25] = "smtp",
        [53] = "dns",
        [80] = "http",
        [443] = "https",
        [445] = "smb",
        [554] = "rtsp",
        [631] = "ipp",
        [1883] = "mqtt",
        [2375] = "docker",
        [2376] = "docker",
        [3000] = "http",
        [3306] = "mysql",
        [3389] = "rdp",
        [5000] = "http",
        [5432] = "postgres",
        [5900] = "vnc",
        [6379] = "redis",
        [7860] = "http",
        [8000] = "http",
        [8006] = "proxmox",
        [8080] = "http",
        [8096] = "jellyfin",
        [8123] = "home-assistant",
        [8443] = "https",
        [9000] = "http",
        [9090] = "http",
        [9100] = "jetdirect",
        [11434] = "ollama",
        [32400] = "plex"
    };

    /// <summary>
    ///     The conventional name for a port, or <c>tcp-1234</c> when nobody curated one —
    ///     which still says more than the bare number, and stays stable across runs.
    /// </summary>
    public static string NameFor(int port) =>
        _names.TryGetValue(port, out var name) ? name : $"tcp-{port}";
}
