using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.ExporterTests;

[Collection("Yaml CLI tests")]
public class SshExportWorkflowTests(
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
    public async Task ssh_export_basic_workflow_test() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
                                                                           version: 1
                                                                           resources:
                                                                           - kind: System
                                                                             type: vm
                                                                             name: vm-web01
                                                                             tags:
                                                                             - prod
                                                                             labels:
                                                                               ip: 192.168.1.10
                                                                               ssh_user: ubuntu

                                                                           - kind: System
                                                                             type: vm
                                                                             name: vm-db01
                                                                             labels:
                                                                               ip: 192.168.1.20
                                                                               ssh_user: postgres
                                                                           """);

        (var output, var _) = await ExecuteAsync(
            "ssh", "export"
        );

        Assert.Equal("""
                     Generated SSH Config:

                     Host vm-db01
                       HostName 192.168.1.20
                       User postgres

                     Host vm-web01
                       HostName 192.168.1.10
                       User ubuntu
                     """, output);
    }

    [Fact]
    public async Task ssh_export_with_defaults_test() {
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
            "ssh", "export",
            "--default-user", "admin",
            "--default-port", "2222",
            "--default-identity", "~/.ssh/id_rsa"
        );

        Assert.Equal("""
                     Generated SSH Config:

                     Host vm1
                       HostName 10.0.0.1
                       User admin
                       Port 2222
                       IdentityFile ~/.ssh/id_rsa
                     """, output);
    }

    [Fact]
    public async Task ssh_export_respects_tag_filter() {
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
            "ssh", "export",
            "--include-tags", "prod"
        );

        Assert.Contains("Host prod-vm", output);
        Assert.DoesNotContain("Host staging-vm", output);
    }

    [Fact]
    public async Task ssh_export_is_sorted_by_name() {
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
            "ssh", "export"
        );

        var aIndex = output.IndexOf("Host a-host", StringComparison.Ordinal);
        var bIndex = output.IndexOf("Host b-host", StringComparison.Ordinal);

        Assert.True(aIndex < bIndex);
    }

    [Fact]
    public async Task ssh_export_skips_resources_without_address() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
                                                                           version: 1
                                                                           resources:
                                                                           - kind: System
                                                                             type: vm
                                                                             name: vm-with-ip
                                                                             labels:
                                                                               ip: 10.0.0.1

                                                                           - kind: System
                                                                             type: vm
                                                                             name: vm-no-ip
                                                                           """);

        (var output, var _) = await ExecuteAsync(
            "ssh", "export"
        );

        Assert.Contains("Host vm-with-ip", output);
        Assert.DoesNotContain("vm-no-ip", output);
    }
}
