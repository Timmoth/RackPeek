using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.OtherTests;

[Collection("Process-wide static state")]
public class OtherPortWorkflowTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
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
    public async Task other_port_cli_workflow_test() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        await ExecuteAsync("other", "add", "decoder01");
        await ExecuteAsync(
            "other", "set", "decoder01",
            "--model", "TVIP-v605",
            "--description", "IPTV set-top box"
        );

        (var output, var yaml) = await ExecuteAsync(
            "other", "port", "add", "decoder01",
            "--type", "rj45",
            "--speed", "0.1",
            "--count", "1"
        );
        Assert.Equal("Port added to other hardware 'decoder01'.\n", output);

        (output, yaml) = await ExecuteAsync(
            "other", "port", "add", "decoder01",
            "--type", "usb",
            "--speed", "0.48",
            "--count", "2"
        );
        Assert.Equal("Port added to other hardware 'decoder01'.\n", output);

        Assert.Equal("""
                     version: 4
                     resources:
                     - kind: Other
                       model: TVIP-v605
                       description: IPTV set-top box
                       ports:
                       - type: rj45
                         speed: 0.1
                         count: 1
                       - type: usb
                         speed: 0.48
                         count: 2
                       name: decoder01
                     connections: []

                     """, yaml);

        (output, yaml) = await ExecuteAsync(
            "other", "port", "set", "decoder01",
            "--index", "1",
            "--type", "usb",
            "--speed", "0.48",
            "--count", "3"
        );
        Assert.Equal("Port #1 updated on other hardware 'decoder01'.\n", output);
        Assert.Contains("count: 3", yaml);

        (output, yaml) = await ExecuteAsync("other", "describe", "decoder01");
        Assert.Contains("Ports:", output);
        Assert.Contains("rj45: 1", output);
        Assert.Contains("usb: 3", output);

        (output, yaml) = await ExecuteAsync("other", "port", "del", "decoder01", "--index", "0");
        Assert.Equal("Port #0 removed from other hardware 'decoder01'.\n", output);

        Assert.Equal("""
                     version: 4
                     resources:
                     - kind: Other
                       model: TVIP-v605
                       description: IPTV set-top box
                       ports:
                       - type: usb
                         speed: 0.48
                         count: 3
                       name: decoder01
                     connections: []

                     """, yaml);
    }

    [Fact]
    public async Task describe_reports_none_when_other_has_no_ports() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        await ExecuteAsync("other", "add", "bare01");

        (var output, var _) = await ExecuteAsync("other", "describe", "bare01");

        Assert.Contains("Ports:", output);
        Assert.Contains("None", output);
    }
}
