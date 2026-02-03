using System.Text;

namespace RackPeek.Domain.Resources.Services;

public class Service : Resource
{
    public Network? Network { get; set; }
    public string? RunsOn { get; set; }
    public const string KindLabel = "Service";

    public string NetworkString()
    {
        if (Network == null) return string.Empty;

        if (!string.IsNullOrEmpty(Network.Url)) return Network.Url;

        var stringBuilder = new StringBuilder();
        if (!string.IsNullOrEmpty(Network.Ip))
        {
            stringBuilder.Append("Ip: ");
            stringBuilder.Append(Network.Ip);
            if (Network.Port.HasValue)
            {
                stringBuilder.Append(':');
                stringBuilder.Append(Network.Port.Value);
            }

            stringBuilder.Append(' ');
        }

        return stringBuilder.ToString();
    }
}

public class Network
{
    public string? Ip { get; set; }
    public int? Port { get; set; }
    public string? Protocol { get; set; }
    public string? Url { get; set; }
    
    
}