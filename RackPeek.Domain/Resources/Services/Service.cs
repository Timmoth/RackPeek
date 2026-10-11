namespace RackPeek.Domain.Resources.Services;

public class Service : Resource {
    public const string KindLabel = "Service";
    public Network? Network { get; set; }

    /// <summary>
    ///     Where this service answers, for showing to a person. Display text, never a
    ///     link — <see cref="BrowsableUrl" /> is the link.
    /// </summary>
    public string NetworkString() =>
        !string.IsNullOrEmpty(Network?.Url)
            ? Network.Url
            : ServiceEndpoint.Describe(Network);

    /// <summary>
    ///     A link a browser can follow, or null when this port serves something a browser
    ///     cannot open.
    /// </summary>
    public string? BrowsableUrl(string? fallbackIp = null) =>
        ServiceEndpoint.BrowsableUrl(Network, fallbackIp);
}

public class Network {
    public string? Ip { get; set; }
    public int? Port { get; set; }
    public string? Protocol { get; set; }
    public string? Url { get; set; }
}
