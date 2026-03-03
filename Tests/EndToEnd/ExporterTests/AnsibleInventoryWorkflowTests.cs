using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.ExporterTests;

[Collection("Yaml CLI tests")]
public class AnsibleInventoryWorkflowTests(
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
    public async Task ansible_inventory_cli_workflow_test() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
                                                                           version: 1
                                                                           resources:
                                                                           - kind: System
                                                                             type: vm
                                                                             os: ubuntu-22.04
                                                                             cores: 2
                                                                             ram: 4
                                                                             name: vm-web01
                                                                             tags:
                                                                             - prod
                                                                             labels:
                                                                               ansible_host: 192.168.1.10
                                                                               ansible_user: ubuntu
                                                                               env: prod

                                                                           - kind: System
                                                                             type: vm
                                                                             os: debian-12
                                                                             cores: 2
                                                                             ram: 2
                                                                             name: vm-staging01
                                                                             tags:
                                                                             - staging
                                                                             labels:
                                                                               ansible_host: 192.168.1.20
                                                                               ansible_user: debian
                                                                               env: staging

                                                                           """);

        (var output, var yaml) = await ExecuteAsync(
            "ansible", "inventory",
            "--group-tags", "prod,staging",
            "--group-labels", "env",
            "--global-var", "ansible_user=ansible",
            "--global-var", "ansible_python_interpreter=/usr/bin/python3"
        );

        Assert.Equal("""
                     Generated Inventory:

                     [all:vars]
                     ansible_python_interpreter=/usr/bin/python3
                     ansible_user=ansible

                     [env_prod]
                     vm-web01 ansible_host=192.168.1.10 ansible_user=ubuntu

                     [env_staging]
                     vm-staging01 ansible_host=192.168.1.20 ansible_user=debian

                     [prod]
                     vm-web01 ansible_host=192.168.1.10 ansible_user=ubuntu

                     [staging]
                     vm-staging01 ansible_host=192.168.1.20 ansible_user=debian
                     """, output);
    }

    [Fact]
    public async Task ansible_inventory_yaml_output_test() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
                                                                           version: 1
                                                                           resources:
                                                                           - kind: System
                                                                             type: vm
                                                                             os: ubuntu-22.04
                                                                             cores: 2
                                                                             ram: 4
                                                                             name: vm-web01
                                                                             tags:
                                                                             - prod
                                                                             labels:
                                                                               ansible_host: 192.168.1.10
                                                                               ansible_user: ubuntu
                                                                               env: prod
                                                                           """);

        (var output, var _) = await ExecuteAsync(
            "ansible", "inventory",
            "--format", "yaml",
            "--group-tags", "prod",
            "--group-labels", "env"
        );

        Assert.Equal("""
                     Generated Inventory:

                     all:
                       children:
                         env_prod:
                           hosts:
                             vm-web01:
                               ansible_host: 192.168.1.10
                               ansible_user: ubuntu
                         prod:
                           hosts:
                             vm-web01:
                               ansible_host: 192.168.1.10
                               ansible_user: ubuntu
                     """, output);
    }

    [Fact]
    public async Task ansible_inventory_groups_are_sorted() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
                                                                           version: 1
                                                                           resources:
                                                                           - kind: System
                                                                             type: vm
                                                                             name: b-host
                                                                             tags: [zeta]
                                                                             labels:
                                                                               ansible_host: 10.0.0.2

                                                                           - kind: System
                                                                             type: vm
                                                                             name: a-host
                                                                             tags: [alpha]
                                                                             labels:
                                                                               ansible_host: 10.0.0.1
                                                                           """);

        (var output, var _) = await ExecuteAsync(
            "ansible", "inventory",
            "--group-tags", "alpha,zeta"
        );

        var alphaIndex = output.IndexOf("[alpha]", StringComparison.Ordinal);
        var zetaIndex = output.IndexOf("[zeta]", StringComparison.Ordinal);

        Assert.True(alphaIndex < zetaIndex);
    }

    [Fact]
    public async Task yaml_format_does_not_emit_ini_sections() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
                                                                           version: 1
                                                                           resources:
                                                                           - kind: System
                                                                             type: vm
                                                                             name: vm1
                                                                             labels:
                                                                               ansible_host: 10.0.0.1
                                                                           """);

        (var output, var _) = await ExecuteAsync(
            "ansible", "inventory",
            "--format", "yaml"
        );

        Assert.DoesNotContain("[", output);
        Assert.Contains("all:", output);
    }
}
