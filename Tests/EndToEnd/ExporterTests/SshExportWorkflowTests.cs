using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.ExporterTests;

[Collection("Process-wide static state")]
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
    public async Task ssh_export_uses_hostname_label_when_no_ip() {
        // ssh-config-export.md §1 & §9: `hostname` is an accepted address
        // fallback when `ip` is not set.
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
                                                                           version: 1
                                                                           resources:
                                                                           - kind: System
                                                                             type: vm
                                                                             name: vm-host
                                                                             labels:
                                                                               hostname: vm-host.lan
                                                                           """);

        (var output, var _) = await ExecuteAsync("ssh", "export");

        Assert.Contains("Host vm-host", output);
        Assert.Contains("HostName vm-host.lan", output);
    }

    [Fact]
    public async Task ssh_export_uses_ansible_host_label_when_no_ip_or_hostname() {
        // ssh-config-export.md §1 & §9: `ansible_host` is the third fallback
        // for the address.
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
                                                                           version: 1
                                                                           resources:
                                                                           - kind: System
                                                                             type: vm
                                                                             name: vm-ansible
                                                                             labels:
                                                                               ansible_host: 10.0.0.99
                                                                           """);

        (var output, var _) = await ExecuteAsync("ssh", "export");

        Assert.Contains("Host vm-ansible", output);
        Assert.Contains("HostName 10.0.0.99", output);
    }

    [Fact]
    public async Task ssh_export_falls_back_from_ssh_user_to_ansible_user() {
        // ssh-config-export.md §9 fallback chain: ssh_user → ansible_user →
        // CLI default. A resource with only `ansible_user` should still
        // produce a User line.
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
                                                                           version: 1
                                                                           resources:
                                                                           - kind: System
                                                                             type: vm
                                                                             name: vm-prefer
                                                                             labels:
                                                                               ip: 10.0.0.1
                                                                               ssh_user: prefer-this
                                                                               ansible_user: ignore-this

                                                                           - kind: System
                                                                             type: vm
                                                                             name: vm-fallback
                                                                             labels:
                                                                               ip: 10.0.0.2
                                                                               ansible_user: from-ansible
                                                                           """);

        (var output, var _) = await ExecuteAsync("ssh", "export");

        Assert.Contains("""
                        Host vm-fallback
                          HostName 10.0.0.2
                          User from-ansible
                        """, output);
        Assert.Contains("""
                        Host vm-prefer
                          HostName 10.0.0.1
                          User prefer-this
                        """, output);
        Assert.DoesNotContain("ignore-this", output);
    }

    [Fact]
    public async Task ssh_export_falls_back_from_ssh_port_to_ansible_port() {
        // ssh-config-export.md §9 fallback chain: ssh_port → ansible_port →
        // CLI default. Note: the generator omits `Port` if the resolved port
        // equals 22, so test with a non-default value.
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
                                                                           version: 1
                                                                           resources:
                                                                           - kind: System
                                                                             type: vm
                                                                             name: vm-prefer
                                                                             labels:
                                                                               ip: 10.0.0.1
                                                                               ssh_port: "2200"
                                                                               ansible_port: "9999"

                                                                           - kind: System
                                                                             type: vm
                                                                             name: vm-fallback
                                                                             labels:
                                                                               ip: 10.0.0.2
                                                                               ansible_port: "2222"
                                                                           """);

        (var output, var _) = await ExecuteAsync("ssh", "export");

        Assert.Contains("""
                        Host vm-fallback
                          HostName 10.0.0.2
                          Port 2222
                        """, output);
        Assert.Contains("""
                        Host vm-prefer
                          HostName 10.0.0.1
                          Port 2200
                        """, output);
        Assert.DoesNotContain("9999", output);
    }

    [Fact]
    public async Task ssh_export_honours_per_resource_ssh_port_and_identity_file_labels() {
        // ssh-config-export.md §2: per-resource ssh_port and
        // ssh_identity_file labels are emitted as Port / IdentityFile lines
        // even when no CLI defaults are passed.
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
                                                                           version: 1
                                                                           resources:
                                                                           - kind: System
                                                                             type: vm
                                                                             name: vm-custom
                                                                             labels:
                                                                               ip: 10.0.0.1
                                                                               ssh_user: ubuntu
                                                                               ssh_port: "2222"
                                                                               ssh_identity_file: ~/.ssh/id_custom
                                                                           """);

        (var output, var _) = await ExecuteAsync("ssh", "export");

        Assert.Contains("""
                        Host vm-custom
                          HostName 10.0.0.1
                          User ubuntu
                          Port 2222
                          IdentityFile ~/.ssh/id_custom
                        """, output);
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
