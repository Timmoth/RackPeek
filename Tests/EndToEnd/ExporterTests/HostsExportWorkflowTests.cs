using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.ExporterTests;

[Collection("Process-wide static state")]
public class HostsExportWorkflowTests(
    TempYamlCliFixture fs,
    ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture> {
    private async Task<(string output, string yaml)> ExecuteAsync(params string[] args) {
        outputHelper.WriteLine($"rpk {string.Join(" ", args)}");

        var output = await YamlCliTestHost.RunAsync(
            args,
            fs.Root,
            outputHelper,
            "config.yaml");

        outputHelper.WriteLine(output);

        var yaml = await File.ReadAllTextAsync(Path.Combine(fs.Root, "config.yaml"));
        return (output, yaml);
    }

    [Fact]
    public async Task hosts_export_basic_workflow_test() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
                                                                           version: 1
                                                                           resources:
                                                                           - kind: System
                                                                             type: vm
                                                                             name: vm-web01
                                                                             labels:
                                                                               ip: 192.168.1.10

                                                                           - kind: System
                                                                             type: vm
                                                                             name: vm-db01
                                                                             labels:
                                                                               ip: 192.168.1.20
                                                                           """);

        (var output, var _) = await ExecuteAsync(
            "hosts", "export",
            "--no-header",
            "--no-localhost"
        );

        Assert.Equal("""
                     Generated Hosts File:

                     192.168.1.20 vm-db01
                     192.168.1.10 vm-web01
                     """, output);
    }

    [Fact]
    public async Task hosts_export_with_domain_suffix_test() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
                                                                           version: 1
                                                                           resources:
                                                                           - kind: System
                                                                             type: vm
                                                                             name: vm1
                                                                             labels:
                                                                               ip: 10.0.0.1
                                                                           """);

        (var output, var _) = await ExecuteAsync(
            "hosts", "export",
            "--domain-suffix", "home.local",
            "--no-header",
            "--no-localhost"
        );

        Assert.Equal("""
                     Generated Hosts File:

                     10.0.0.1 vm1.home.local
                     """, output);
    }

    [Fact]
    public async Task hosts_export_respects_tag_filter() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
                                                                           version: 1
                                                                           resources:
                                                                           - kind: System
                                                                             type: vm
                                                                             name: prod-vm
                                                                             tags:
                                                                             - prod
                                                                             labels:
                                                                               ip: 10.0.0.1

                                                                           - kind: System
                                                                             type: vm
                                                                             name: staging-vm
                                                                             tags:
                                                                             - staging
                                                                             labels:
                                                                               ip: 10.0.0.2
                                                                           """);

        (var output, var _) = await ExecuteAsync(
            "hosts", "export",
            "--include-tags", "prod",
            "--no-header",
            "--no-localhost"
        );

        Assert.Contains("10.0.0.1 prod-vm", output);
        Assert.DoesNotContain("staging-vm", output);
    }

    [Fact]
    public async Task hosts_export_is_sorted_by_name() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
                                                                           version: 1
                                                                           resources:
                                                                           - kind: System
                                                                             type: vm
                                                                             name: b-host
                                                                             labels:
                                                                               ip: 10.0.0.2

                                                                           - kind: System
                                                                             type: vm
                                                                             name: a-host
                                                                             labels:
                                                                               ip: 10.0.0.1
                                                                           """);

        (var output, var _) = await ExecuteAsync(
            "hosts", "export",
            "--no-header",
            "--no-localhost"
        );

        var aIndex = output.IndexOf("a-host", StringComparison.Ordinal);
        var bIndex = output.IndexOf("b-host", StringComparison.Ordinal);

        Assert.True(aIndex < bIndex);
    }

    [Fact]
    public async Task hosts_export_uses_hostname_label_when_no_ip() {
        // hosts-file-export.md §1: the `hostname` label is an alternative
        // address. A resource with only `hostname` should still appear, with
        // the hostname taking the address column.
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
                                                                           version: 1
                                                                           resources:
                                                                           - kind: System
                                                                             type: vm
                                                                             name: vm-hostname
                                                                             labels:
                                                                               hostname: vm-hostname.lan
                                                                           """);

        (var output, var _) = await ExecuteAsync(
            "hosts", "export",
            "--no-header",
            "--no-localhost"
        );

        Assert.Contains("vm-hostname.lan vm-hostname", output);
    }

    [Fact]
    public async Task hosts_export_uses_ansible_host_label_when_no_ip_or_hostname() {
        // hosts-file-export.md §1: "If you already use Ansible, `ansible_host`
        // also works." A resource with only ansible_host should appear in
        // the hosts file.
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
                                                                           version: 1
                                                                           resources:
                                                                           - kind: System
                                                                             type: vm
                                                                             name: vm-ansible
                                                                             labels:
                                                                               ansible_host: 10.0.0.99
                                                                           """);

        (var output, var _) = await ExecuteAsync(
            "hosts", "export",
            "--no-header",
            "--no-localhost"
        );

        Assert.Contains("10.0.0.99 vm-ansible", output);
    }

    [Fact]
    public async Task hosts_export_prefers_ip_over_hostname_and_ansible_host() {
        // hosts-file-export.md implies a precedence: ip is the canonical
        // address, with hostname/ansible_host only as fallbacks. When all
        // three are present the `ip` value wins.
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
                                                                           version: 1
                                                                           resources:
                                                                           - kind: System
                                                                             type: vm
                                                                             name: vm-all
                                                                             labels:
                                                                               ip: 10.0.0.1
                                                                               hostname: not-this.lan
                                                                               ansible_host: 10.0.0.2
                                                                           """);

        (var output, var _) = await ExecuteAsync(
            "hosts", "export",
            "--no-header",
            "--no-localhost"
        );

        Assert.Contains("10.0.0.1 vm-all", output);
        Assert.DoesNotContain("not-this.lan", output);
        Assert.DoesNotContain("10.0.0.2", output);
    }

    [Fact]
    public async Task hosts_export_skips_resources_without_address() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
                                                                           version: 1
                                                                           resources:
                                                                           - kind: System
                                                                             type: vm
                                                                             name: with-ip
                                                                             labels:
                                                                               ip: 10.0.0.1

                                                                           - kind: System
                                                                             type: vm
                                                                             name: without-ip
                                                                           """);

        (var output, var _) = await ExecuteAsync(
            "hosts", "export",
            "--no-header",
            "--no-localhost"
        );

        Assert.Contains("10.0.0.1 with-ip", output);
        Assert.DoesNotContain("without-ip", output);
    }
}
