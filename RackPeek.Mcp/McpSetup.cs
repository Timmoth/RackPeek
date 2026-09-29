using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using RackPeek.Mcp.Tools;

namespace RackPeek.Mcp;

public static class McpSetup {
    /// <summary>The name MCP clients see in the initialize handshake.</summary>
    public const string ServerName = "rackpeek";

    public static IMcpServerBuilder WithRackPeekTools(this IMcpServerBuilder builder) {
        // MCP tool serialization does not go through ASP.NET's ConfigureHttpJsonOptions,
        // so enums-as-strings is opted into again here to match the REST API's JSON.
        var json = new JsonSerializerOptions(McpJsonUtilities.DefaultOptions);
        json.Converters.Add(new JsonStringEnumConverter());

        return builder
            .WithTools<QueryTools>(json)
            .WithTools<MutationTools>(json)
            .WithTools<ExportTools>(json)
            .WithTools<GitTools>(json)
            .WithTools<DiscoveryTools>(json);
    }
}
