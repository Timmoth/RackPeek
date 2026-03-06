using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.ConnectionTests;

[Collection("Yaml CLI tests")]
public class ConnectionRemoveWorkflowTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
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
    public async Task connections_remove_cli_workflow_test(string aType, string bType) {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Create resources
        await ExecuteAsync(aType, "add", "node-a");
        await ExecuteAsync(bType, "add", "node-b");

        // Add ports
        await ExecuteAsync(
            aType, "port", "add", "node-a",
            "--type", "RJ45",
            "--speed", "1",
            "--count", "2"
        );

        await ExecuteAsync(
            bType, "port", "add", "node-b",
            "--type", "RJ45",
            "--speed", "1",
            "--count", "2"
        );

        // Create connection
        await ExecuteAsync(
            "connections", "add",
            "node-a", "0", "0",
            "node-b", "0", "0"
        );

        // Remove connection
        (var output, var yaml) = await ExecuteAsync(
            "connections", "remove",
            "node-a", "0", "0"
        );

        Assert.Contains("Connection removed", output);

        // YAML should no longer contain connection
        outputHelper.WriteLine(yaml);
        Assert.Contains("connections: []", yaml);
    }

    [Fact]
    public async Task removing_connection_from_other_endpoint_works() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        await ExecuteAsync("servers", "add", "srv01");
        await ExecuteAsync("servers", "add", "srv02");

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
            "connections", "add",
            "srv01", "0", "0",
            "srv02", "0", "0"
        );

        // Remove using other side
        (var output, var yaml) = await ExecuteAsync(
            "connections", "remove",
            "srv02", "0", "0"
        );

        Assert.Contains("Connection removed", output);

        outputHelper.WriteLine(yaml);
        Assert.Contains("connections: []", yaml);
    }

    [Fact]
    public async Task removing_nonexistent_connection_is_safe() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        await ExecuteAsync("switches", "add", "sw01");

        await ExecuteAsync(
            "switches", "port", "add", "sw01",
            "--type", "RJ45",
            "--speed", "1",
            "--count", "2"
        );

        (var output, _) = await ExecuteAsync(
            "connections", "remove",
            "sw01", "0", "0"
        );

        Assert.Contains("Connection removed", output);
    }
}
