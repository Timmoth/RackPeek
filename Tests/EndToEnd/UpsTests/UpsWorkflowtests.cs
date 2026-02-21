using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.UpsTests;

[Collection("Yaml CLI tests")]
public class UpsWorkflowTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture>
{
    private async Task<(string, string)> ExecuteAsync(params string[] args)
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
    public async Task ups_cli_workflow_test()
    {
        // Add UPS
        var (output, yaml) = await ExecuteAsync("ups", "add", "ups01");
        Assert.Equal("UPS 'ups01' added.\n", output);
        Assert.Contains("name: ups01", yaml);

        // Update UPS
        (output, yaml) = await ExecuteAsync(
            "ups", "set", "ups01",
            "--model", "APC-SmartUPS-1500",
            "--va", "1500"
        );
        Assert.Equal("UPS 'ups01' updated.\n", output);

        Assert.Equal("""
                     resources:
                     - kind: Ups
                       model: APC-SmartUPS-1500
                       va: 1500
                       name: ups01

                     """, yaml);

        // Add second UPS
        (output, yaml) = await ExecuteAsync("ups", "add", "ups02");
        Assert.Equal("UPS 'ups02' added.\n", output);

        (output, yaml) = await ExecuteAsync(
            "ups", "set", "ups02",
            "--model", "CyberPower-2200VA",
            "--va", "2200"
        );
        Assert.Equal("UPS 'ups02' updated.\n", output);

        Assert.Equal("""
                     resources:
                     - kind: Ups
                       model: APC-SmartUPS-1500
                       va: 1500
                       name: ups01
                     - kind: Ups
                       model: CyberPower-2200VA
                       va: 2200
                       name: ups02

                     """, yaml);

        // Get UPS
        (output, yaml) = await ExecuteAsync("ups", "get", "ups01");
        Assert.Contains("ups01", output);
        Assert.Contains("APC-SmartUPS-1500", output);
        Assert.Contains("1500", output);

        // List UPS units
        (output, yaml) = await ExecuteAsync("ups", "list");
        Assert.Contains("ups01", output);
        Assert.Contains("ups02", output);

        // Summary
        (output, yaml) = await ExecuteAsync("ups", "summary");
        Assert.Contains("ups01", output);
        Assert.Contains("ups02", output);

        // Delete UPS
        (output, yaml) = await ExecuteAsync("ups", "del", "ups02");
        Assert.Equal("""
                     UPS 'ups02' deleted.

                     """, output);

        // List again
        (output, yaml) = await ExecuteAsync("ups", "list");
        Assert.Contains("ups01", output);
        Assert.DoesNotContain("ups02", output);
    }
}