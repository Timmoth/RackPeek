using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Services.Networking;

namespace RackPeek.Domain.Resources.Services.UseCases;

public class ServiceSubnetsUseCase(IResourceCollection repo) : IUseCase
{
    public async Task<ServiceSubnetsResult> ExecuteAsync(string? cidr, int? prefix, CancellationToken token)
    {
        var services = await repo.GetAllOfTypeAsync<Service>();

        // If CIDR is provided → filter mode
        if (cidr is not null)
        {
            Cidr parsed;
            try
            {
                parsed = Cidr.Parse(cidr);
            }
            catch
            {
                return ServiceSubnetsResult.InvalidCidr(cidr);
            }

            var matches = services
                .Where(s => s.Network?.Ip != null)
                .Where(s => parsed.Contains(IpHelper.ToUInt32(s.Network!.Ip!)))
                .Select(s => new ServiceSummary(
                    s.Name,
                    s.Network!.Ip!,
                    s.RunsOn))
                .ToList();

            return ServiceSubnetsResult.FromServices(matches, parsed.ToString());
        }

        // No CIDR → subnet discovery mode
        var effectivePrefix = prefix ?? 24;
        var mask = IpHelper.MaskFromPrefix(effectivePrefix);

        var groups = services
            .Where(s => s.Network?.Ip != null)
            .Select(s => IpHelper.ToUInt32(s.Network!.Ip!))
            .GroupBy(ip => ip & mask)
            .Select(g => new SubnetSummary(
                $"{IpHelper.ToIp(g.Key)}/{effectivePrefix}",
                g.Count()))
            .OrderBy(s => s.Cidr)
            .ToList();

        return ServiceSubnetsResult.FromSubnets(groups);
    }
}

public record SubnetSummary(string Cidr, int Count);

public record ServiceSummary(string Name, string Ip, string? RunsOn);

public class ServiceSubnetsResult
{
    public bool IsInvalidCidr { get; private set; }
    public string? InvalidCidrValue { get; private set; }

    public string? FilteredCidr { get; private set; }

    public List<SubnetSummary> Subnets { get; private set; } = new();
    public List<ServiceSummary> Services { get; private set; } = new();

    public static ServiceSubnetsResult InvalidCidr(string cidr)
    {
        return new ServiceSubnetsResult { IsInvalidCidr = true, InvalidCidrValue = cidr };
    }

    public static ServiceSubnetsResult FromSubnets(List<SubnetSummary> subnets)
    {
        return new ServiceSubnetsResult { Subnets = subnets };
    }

    public static ServiceSubnetsResult FromServices(List<ServiceSummary> services, string cidr)
    {
        return new ServiceSubnetsResult { Services = services, FilteredCidr = cidr };
    }
}