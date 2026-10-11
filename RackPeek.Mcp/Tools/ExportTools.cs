using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using RackPeek.Domain.Graph;
using RackPeek.Domain.Graph.Serialisers;
using RackPeek.Domain.Graph.UseCases;
using RackPeek.Domain.UseCases.Ansible;
using RackPeek.Domain.UseCases.Hosts;
using RackPeek.Domain.UseCases.SSH;

namespace RackPeek.Mcp.Tools;

public enum TopologyView {
    Physical,
    Logical
}

[McpServerToolType]
public sealed class ExportTools(IServiceProvider services) {
    [McpServerTool(Name = "export_ansible_inventory", UseStructuredContent = true, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Renders the inventory as an Ansible inventory file. A resource needs an 'ansible_host', 'ip' or " +
                 "'hostname' label to be included, and hosts are emitted under the groups built from groupByTags / " +
                 "groupByLabelKeys — pass at least one of those or the inventory comes back empty.")]
    public Task<InventoryResult?> ExportAnsibleInventory(
        [Description("Output format: Ini or Yaml.")] InventoryFormat format = InventoryFormat.Ini,
        [Description("Create a group per listed tag.")] string[]? groupByTags = null,
        [Description("Create groups from these label keys, e.g. 'env' groups hosts into env_prod, env_dev.")]
        string[]? groupByLabelKeys = null) {
        return ToolErrors.RunAsync(() =>
            services.GetRequiredService<AnsibleInventoryGeneratorUseCase>().ExecuteAsync(new InventoryOptions {
                Format = format,
                GroupByTags = groupByTags ?? [],
                GroupByLabelKeys = groupByLabelKeys ?? []
            }));
    }

    [McpServerTool(Name = "export_ssh_config", UseStructuredContent = true, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Renders the inventory as an OpenSSH client config (Host blocks).")]
    public Task<SshExportResult?> ExportSshConfig(
        [Description("Only include resources carrying at least one of these tags.")]
        string[]? includeTags = null,
        [Description("Default SSH user for every host.")] string? defaultUser = null,
        [Description("Default SSH port for every host.")] int defaultPort = 22,
        [Description("Default IdentityFile path for every host.")] string? defaultIdentityFile = null) {
        return ToolErrors.RunAsync(() =>
            services.GetRequiredService<SshConfigExportUseCase>().ExecuteAsync(new SshExportOptions {
                IncludeTags = includeTags ?? [],
                DefaultUser = defaultUser,
                DefaultPort = defaultPort,
                DefaultIdentityFile = defaultIdentityFile
            }));
    }

    [McpServerTool(Name = "export_hosts_file", UseStructuredContent = true, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Renders the inventory as /etc/hosts entries.")]
    public Task<HostsExportResult?> ExportHostsFile(
        [Description("Only include resources carrying at least one of these tags.")]
        string[]? includeTags = null,
        [Description("Domain suffix appended to every host name, e.g. 'home.local'.")]
        string? domainSuffix = null,
        [Description("Include the localhost entries at the top.")]
        bool includeLocalhostDefaults = true) {
        return ToolErrors.RunAsync(() =>
            services.GetRequiredService<HostsFileExportUseCase>().ExecuteAsync(new HostsExportOptions {
                IncludeTags = includeTags ?? [],
                DomainSuffix = domainSuffix,
                IncludeLocalhostDefaults = includeLocalhostDefaults
            }));
    }

    [McpServerTool(Name = "export_topology_mermaid", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Renders the infrastructure as a Mermaid diagram: Physical shows hardware and its port connections, Logical shows host cards with their services grouped by subnet.")]
    public Task<string> ExportTopologyMermaid(
        [Description("Physical or Logical.")] TopologyView view = TopologyView.Physical) {
        return ToolErrors.RunAsync(async () => {
            Graph graph = view == TopologyView.Physical
                ? await services.GetRequiredService<BuildPhysicalTopologyUseCase>().ExecuteAsync()
                : await services.GetRequiredService<BuildLogicalGraphUseCase>().ExecuteAsync();

            return new MermaidSerialiser().Serialise(graph);
        });
    }
}
