using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.ConnectionTests;

[Collection("Yaml CLI tests")]
public class ConnectionWorkflowTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
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

    [Theory]
    [InlineData("switches", "routers")]
    [InlineData("firewalls", "routers")]
    public async Task connections_cli_workflow_test(string aType, string bType) {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Create resources
        await ExecuteAsync(aType, "add", "node-a");
        await ExecuteAsync(bType, "add", "node-b");

        // Add NIC to A
        await ExecuteAsync(
            aType, "port", "add", "node-a",
            "--type", "RJ45",
            "--speed", "1",
            "--count", "2"
        );

        // Add NIC to B
        await ExecuteAsync(
            bType, "port", "add", "node-b",
            "--type", "RJ45",
            "--speed", "1",
            "--count", "2"
        );

        // Create connection
        (var output, var yaml) = await ExecuteAsync(
            "connections", "add",
            "node-a", "0", "0",
            "node-b", "0", "0",
            "--label", "uplink"
        );

        Assert.Contains("Connection created", output);

        // YAML validation
        Assert.Contains("connections:", yaml);
        Assert.Contains("node-a", yaml);
        Assert.Contains("node-b", yaml);
        Assert.Contains("uplink", yaml);
    }

    [Fact]
    public async Task connections_overwrite_existing_port_connection() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        await ExecuteAsync("servers", "add", "srv01");
        await ExecuteAsync("servers", "add", "srv02");
        await ExecuteAsync("servers", "add", "srv03");

        await ExecuteAsync(
            "servers", "nic", "add", "srv01",
            "--type", "RJ45",
            "--speed", "1",
            "--ports", "1"
        );

        await ExecuteAsync(
            "servers", "nic", "add", "srv02",
            "--type", "RJ45",
            "--speed", "1",
            "--ports", "1"
        );

        await ExecuteAsync(
            "servers", "nic", "add", "srv03",
            "--type", "RJ45",
            "--speed", "1",
            "--ports", "1"
        );

        // First connection
        await ExecuteAsync(
            "connections", "add",
            "srv01", "0", "0",
            "srv02", "0", "0"
        );

        // Overwrite by connecting srv01 to srv03
        (var output, var yaml) = await ExecuteAsync(
            "connections", "add",
            "srv01", "0", "0",
            "srv03", "0", "0"
        );

        Assert.Contains("Connection created", output);

        // YAML should contain srv01 <-> srv03
        Assert.Contains("srv03", yaml);

        // srv02 should no longer be connected
        Assert.DoesNotContain("srv02\n  portGroup: 0\n  portIndex: 0", yaml);
    }

    [Fact]
    public async Task connections_cannot_connect_port_to_itself() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        await ExecuteAsync("servers", "add", "srv01");

        await ExecuteAsync(
            "servers", "nic", "add", "srv01",
            "--type", "RJ45",
            "--speed", "1",
            "--ports", "2"
        );

        var output = await YamlCliTestHost.RunAsync(
            new[] { "connections", "add", "srv01", "0", "0", "srv01", "0", "0" },
            fs.Root,
            outputHelper,
            "config.yaml");

        Assert.Contains("Cannot connect a port to itself", output);
    }
}
