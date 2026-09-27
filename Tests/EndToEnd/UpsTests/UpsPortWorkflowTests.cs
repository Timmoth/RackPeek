using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.UpsTests;

[Collection("Yaml CLI tests")]
public class UpsPortWorkflowTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture> {
    private async Task<(string, string)> ExecuteAsync(params string[] args) {
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
    public async Task ups_port_cli_workflow_test() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        await ExecuteAsync("ups", "add", "ups01");
        await ExecuteAsync("ups", "set", "ups01", "--model", "APC-BGM2200", "--va", "2200");

        // A UPS data port that is physically RJ45-shaped but enumerates as USB.
        (var output, var yaml) = await ExecuteAsync(
            "ups", "port", "add", "ups01",
            "--type", "usb",
            "--speed", "0.48",
            "--count", "1"
        );
        Assert.Equal("Port added to UPS 'ups01'.\n", output);

        // The dataline surge pass-through pair.
        (output, yaml) = await ExecuteAsync(
            "ups", "port", "add", "ups01",
            "--type", "rj45",
            "--speed", "1",
            "--count", "2"
        );
        Assert.Equal("Port added to UPS 'ups01'.\n", output);

        Assert.Equal("""
                     version: 4
                     resources:
                     - kind: Ups
                       model: APC-BGM2200
                       va: 2200
                       ports:
                       - type: usb
                         speed: 0.48
                         count: 1
                       - type: rj45
                         speed: 1
                         count: 2
                       name: ups01
                     connections: []

                     """, yaml);

        // Update the second group in place.
        (output, yaml) = await ExecuteAsync(
            "ups", "port", "set", "ups01",
            "--index", "1",
            "--type", "rj45",
            "--speed", "1",
            "--count", "4"
        );
        Assert.Equal("Port #1 updated on UPS 'ups01'.\n", output);
        Assert.Contains("count: 4", yaml);

        // Describe surfaces the port summary.
        (output, yaml) = await ExecuteAsync("ups", "describe", "ups01");
        Assert.Contains("Ports:", output);
        Assert.Contains("usb: 1", output);
        Assert.Contains("rj45: 4", output);

        // Remove the pass-through pair again.
        (output, yaml) = await ExecuteAsync("ups", "port", "del", "ups01", "--index", "1");
        Assert.Equal("Port #1 removed from UPS 'ups01'.\n", output);

        Assert.Equal("""
                     version: 4
                     resources:
                     - kind: Ups
                       model: APC-BGM2200
                       va: 2200
                       ports:
                       - type: usb
                         speed: 0.48
                         count: 1
                       name: ups01
                     connections: []

                     """, yaml);

        (output, yaml) = await ExecuteAsync("ups", "describe", "ups01");
        Assert.Contains("usb: 1", output);
        Assert.DoesNotContain("rj45", output);
    }

    [Fact]
    public async Task describe_reports_none_when_ups_has_no_ports() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        await ExecuteAsync("ups", "add", "ups-bare");

        (var output, var _) = await ExecuteAsync("ups", "describe", "ups-bare");

        Assert.Contains("Ports:", output);
        Assert.Contains("None", output);
    }
}
