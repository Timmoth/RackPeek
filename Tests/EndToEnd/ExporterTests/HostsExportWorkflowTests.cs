using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.ExporterTests;

[Collection("Yaml CLI tests")]
public class HostsExportWorkflowTests(
    TempYamlCliFixture fs,
    ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture>
{
    private async Task<(string output, string yaml)> ExecuteAsync(params string[] args)
    {
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
    public async Task hosts_export_basic_workflow_test()
    {
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

        var (output, _) = await ExecuteAsync(
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
    public async Task hosts_export_with_domain_suffix_test()
    {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
                                                                           version: 1
                                                                           resources:
                                                                           - kind: System
                                                                             type: vm
                                                                             name: vm1
                                                                             labels:
                                                                               ip: 10.0.0.1
                                                                           """);

        var (output, _) = await ExecuteAsync(
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
    public async Task hosts_export_respects_tag_filter()
    {
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

        var (output, _) = await ExecuteAsync(
            "hosts", "export",
            "--include-tags", "prod",
            "--no-header",
            "--no-localhost"
        );

        Assert.Contains("10.0.0.1 prod-vm", output);
        Assert.DoesNotContain("staging-vm", output);
    }

    [Fact]
    public async Task hosts_export_is_sorted_by_name()
    {
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

        var (output, _) = await ExecuteAsync(
            "hosts", "export",
            "--no-header",
            "--no-localhost"
        );

        var aIndex = output.IndexOf("a-host", StringComparison.Ordinal);
        var bIndex = output.IndexOf("b-host", StringComparison.Ordinal);

        Assert.True(aIndex < bIndex);
    }

    [Fact]
    public async Task hosts_export_skips_resources_without_address()
    {
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

        var (output, _) = await ExecuteAsync(
            "hosts", "export",
            "--no-header",
            "--no-localhost"
        );

        Assert.Contains("10.0.0.1 with-ip", output);
        Assert.DoesNotContain("without-ip", output);
    }
}
