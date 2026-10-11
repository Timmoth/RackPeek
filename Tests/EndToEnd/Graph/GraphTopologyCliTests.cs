using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.Graph;

[Collection("Process-wide static state")]
public class GraphTopologyCliTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture> {
    private async Task<string> ExecuteAsync(params string[] args) {
        outputHelper.WriteLine($"rpk {string.Join(" ", args)}");
        var output = await YamlCliTestHost.RunAsync(args, fs.Root, outputHelper, "config.yaml");
        outputHelper.WriteLine(output);
        return output;
    }

    [Fact]
    public async Task Topology_Includes_All_Hardware_And_Connection() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        await ExecuteAsync("firewalls", "add", "fw-01");
        await ExecuteAsync("switches", "add", "sw-01");
        await ExecuteAsync("servers", "add", "srv-01");

        // Give each resource at least one port group so connections have something to attach to.
        await ExecuteAsync("firewalls", "port", "add", "fw-01", "--type", "RJ45", "--speed", "1", "--count", "4");
        await ExecuteAsync("switches", "port", "add", "sw-01", "--type", "RJ45", "--speed", "1", "--count", "24");
        await ExecuteAsync("servers", "nic", "add", "srv-01", "--type", "RJ45", "--speed", "1", "--ports", "2");

        await ExecuteAsync("connections", "add", "fw-01", "0", "0", "sw-01", "0", "0");

        var output = await ExecuteAsync("graph", "topology");

        Assert.Contains("flowchart TD", output);
        Assert.Contains("fw-01", output);
        Assert.Contains("sw-01", output);
        Assert.Contains("srv-01", output);
        // All nodes share the rpknode class with a kind subtitle.
        Assert.Contains(":::rpknode", output);
        Assert.Contains("fw-01<br/>firewall", output);
        Assert.Contains("sw-01<br/>switch", output);
        Assert.Contains("srv-01<br/>server", output);
        Assert.Contains("n_fw_01 ---", output);
        Assert.Contains("n_sw_01", output);
    }

    [Fact]
    public async Task Topology_With_No_Hardware_Renders_Header_Only() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        var output = await ExecuteAsync("graph", "topology");

        Assert.Contains("flowchart TD", output);
        Assert.Contains("classDef rpknode", output);
        // No actual node entries.
        Assert.DoesNotContain("[\"", output);
    }
}
