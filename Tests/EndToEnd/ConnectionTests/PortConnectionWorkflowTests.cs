using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.ConnectionTests;

[Collection("Yaml CLI tests")]
public class PortConnectionWorkflowTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
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
    public async Task removing_port_removes_connections(string aType, string bType) {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        await ExecuteAsync(aType, "add", "node-a");
        await ExecuteAsync(bType, "add", "node-b");

        await ExecuteAsync(aType, "port", "add", "node-a",
            "--type", "RJ45",
            "--speed", "1",
            "--count", "2");

        await ExecuteAsync(bType, "port", "add", "node-b",
            "--type", "RJ45",
            "--speed", "1",
            "--count", "2");

        await ExecuteAsync("connections", "add",
            "node-a", "0", "0",
            "node-b", "0", "0");

        (var output, var yaml) = await ExecuteAsync(
            aType, "port", "del", "node-a",
            "--index", "0");

        Assert.Contains("Port 0 removed", output);

        Assert.Contains("connections: []", yaml);
    }

    [Theory]
    [InlineData("switches", "routers")]
    [InlineData("firewalls", "routers")]
    public async Task removing_port_shifts_connection_groups(string aType, string bType) {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        await ExecuteAsync(aType, "add", "node-a");
        await ExecuteAsync(bType, "add", "node-b");

        await ExecuteAsync(aType, "port", "add", "node-a",
            "--type", "RJ45",
            "--speed", "1",
            "--count", "1");

        await ExecuteAsync(aType, "port", "add", "node-a",
            "--type", "RJ45",
            "--speed", "1",
            "--count", "1");

        await ExecuteAsync(bType, "port", "add", "node-b",
            "--type", "RJ45",
            "--speed", "1",
            "--count", "1");

        await ExecuteAsync("connections", "add",
            "node-a", "1", "0",
            "node-b", "0", "0");

        await ExecuteAsync(
            aType, "port", "del", "node-a",
            "--index", "0");

        (string output, string yaml) executeAsync = await ExecuteAsync(
            "connections", "add",
            "node-a", "0", "0",
            "node-b", "0", "0");

        Assert.Contains("node-a", executeAsync.yaml);
        Assert.Contains("node-b", executeAsync.yaml);
    }

    [Theory]
    [InlineData("switches", "routers")]
    [InlineData("firewalls", "routers")]
    public async Task shrinking_port_count_removes_connections(string aType, string bType) {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        await ExecuteAsync(aType, "add", "node-a");
        await ExecuteAsync(bType, "add", "node-b");

        await ExecuteAsync(aType, "port", "add", "node-a",
            "--type", "RJ45",
            "--speed", "1",
            "--count", "3");

        await ExecuteAsync(bType, "port", "add", "node-b",
            "--type", "RJ45",
            "--speed", "1",
            "--count", "3");

        await ExecuteAsync("connections", "add",
            "node-a", "0", "2",
            "node-b", "0", "0");

        await ExecuteAsync(
            aType, "port", "set", "node-a",
            "--index", "0",
            "--count", "1");

        (string output, string yaml) executeAsync = await ExecuteAsync(
            "connections", "add",
            "node-a", "0", "0",
            "node-b", "0", "1");

        Assert.Contains("node-a", executeAsync.yaml);
    }

    [Theory]
    [InlineData("switches", "routers")]
    [InlineData("firewalls", "routers")]
    public async Task shrinking_port_count_preserves_valid_connections(string aType, string bType) {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        await ExecuteAsync(aType, "add", "node-a");
        await ExecuteAsync(bType, "add", "node-b");

        await ExecuteAsync(aType, "port", "add", "node-a",
            "--type", "RJ45",
            "--speed", "1",
            "--count", "3");

        await ExecuteAsync(bType, "port", "add", "node-b",
            "--type", "RJ45",
            "--speed", "1",
            "--count", "3");

        await ExecuteAsync("connections", "add",
            "node-a", "0", "0",
            "node-b", "0", "0");

        await ExecuteAsync(
            aType, "port", "set", "node-a",
            "--index", "0",
            "--count", "2");

        (string output, string yaml) executeAsync = await ExecuteAsync(
            "connections", "add",
            "node-a", "0", "1",
            "node-b", "0", "1");

        Assert.Contains("node-a", executeAsync.yaml);
    }
}
