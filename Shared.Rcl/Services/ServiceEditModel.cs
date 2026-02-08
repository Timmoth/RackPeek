using RackPeek.Domain.Resources.Services;

namespace Shared.Rcl.Services;

public sealed class ServiceEditModel
{
    public string Name { get; init; } = default!;
    public string? Ip { get; set; }
    public int? Port { get; set; }
    public string? Protocol { get; set; }
    public string? Url { get; set; }
    public string? RunsOn { get; set; }

    public static ServiceEditModel From(Service s)
    {
        return new ServiceEditModel
        {
            Name = s.Name,
            Ip = s.Network?.Ip,
            Port = s.Network?.Port,
            Protocol = s.Network?.Protocol,
            Url = s.Network?.Url,
            RunsOn = s.RunsOn
        };
    }
}