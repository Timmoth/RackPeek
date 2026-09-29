using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd;

[Collection("Process-wide static state")]
public class DeleteResourceTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
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
    public async Task deleting_resource_removes_connections_from_endpoint_a() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");
        await ExecuteAsync("servers", "add", "srv01");
        await ExecuteAsync("servers", "add", "srv02");

        await ExecuteAsync(
            "servers", "nic", "add", "srv01",
            "--type", "RJ45",
            "--speed", "10",
            "--ports", "2");

        await ExecuteAsync(
            "servers", "nic", "add", "srv02",
            "--type", "RJ45",
            "--speed", "10",
            "--ports", "2");

        await ExecuteAsync(
            "connections", "add",
            "srv01", "0", "0",
            "srv02", "0", "0",
            "--label", "test-connection");

        (var output, var yaml) = await ExecuteAsync("servers", "del", "srv01");

        Assert.Contains("Server 'srv01' deleted.", output);
        // Connection referencing deleted resource is removed
        Assert.DoesNotContain("test-connection", yaml);
        // srv02 should still exist
        Assert.Contains("srv02", yaml);
        Assert.DoesNotContain("srv01", yaml);
    }

    [Fact]
    public async Task deleting_resource_removes_connections_from_endpoint_b() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");
        await ExecuteAsync("servers", "add", "srv01");
        await ExecuteAsync("servers", "add", "srv02");

        await ExecuteAsync(
            "servers", "nic", "add", "srv01",
            "--type", "RJ45",
            "--speed", "10",
            "--ports", "2");

        await ExecuteAsync(
            "servers", "nic", "add", "srv02",
            "--type", "RJ45",
            "--speed", "10",
            "--ports", "2");

        await ExecuteAsync(
            "connections", "add",
            "srv01", "0", "0",
            "srv02", "0", "0",
            "--label", "test-connection");

        (var output, var yaml) = await ExecuteAsync("servers", "del", "srv02");

        Assert.Contains("Server 'srv02' deleted.", output);
        // Connection referencing deleted resource is removed
        Assert.DoesNotContain("test-connection", yaml);
        // srv01 should still exist
        Assert.Contains("srv01", yaml);
        Assert.DoesNotContain("srv02", yaml);
    }

    [Fact]
    public async Task deleting_resource_removes_dependant_runs_on() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");
        await ExecuteAsync("servers", "add", "srv01");
        await ExecuteAsync("systems", "add", "sys01");
        await ExecuteAsync("systems", "set", "sys01", "--runs-on", "srv01");

        (var output, var yaml) = await ExecuteAsync("servers", "del", "srv01");

        Assert.Contains("Server 'srv01' deleted.", output);
        // System should still exist but without runs-on reference
        Assert.Contains("sys01", yaml);
        // The runs-on reference should be removed from the system
        Assert.DoesNotContain("srv01", yaml);
    }

    [Fact]
    public async Task deleting_resource_typed_in_different_case_removes_dependant_runs_on() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");
        await ExecuteAsync("servers", "add", "Srv01");
        await ExecuteAsync("systems", "add", "sys01");
        await ExecuteAsync("systems", "set", "sys01", "--runs-on", "Srv01");

        (var output, var yaml) = await ExecuteAsync("servers", "del", "srv01");

        Assert.Contains("Server 'srv01' deleted.", output);
        Assert.Contains("sys01", yaml);
        // The runs-on reference should be removed even though the name was typed in a different case
        Assert.DoesNotContain("Srv01", yaml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task deleting_resource_typed_in_different_case_removes_connections() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");
        await ExecuteAsync("servers", "add", "Srv01");
        await ExecuteAsync("servers", "add", "Srv02");

        await ExecuteAsync(
            "servers", "nic", "add", "Srv01",
            "--type", "RJ45",
            "--speed", "10",
            "--ports", "2");

        await ExecuteAsync(
            "servers", "nic", "add", "Srv02",
            "--type", "RJ45",
            "--speed", "10",
            "--ports", "2");

        await ExecuteAsync(
            "connections", "add",
            "Srv01", "0", "0",
            "Srv02", "0", "0",
            "--label", "mixed-case-connection");

        (var output, var yaml) = await ExecuteAsync("servers", "del", "srv01");

        Assert.Contains("Server 'srv01' deleted.", output);
        Assert.Contains("Srv02", yaml);
        Assert.DoesNotContain("mixed-case-connection", yaml);
        Assert.DoesNotContain("Srv01", yaml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task deleting_resource_with_multiple_connections_removes_all() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");
        await ExecuteAsync("switches", "add", "sw01");
        await ExecuteAsync("switches", "add", "sw02");
        await ExecuteAsync("switches", "add", "sw03");

        await ExecuteAsync(
            "switches", "port", "add", "sw01",
            "--type", "SFP+",
            "--speed", "25",
            "--count", "3");

        await ExecuteAsync(
            "switches", "port", "add", "sw02",
            "--type", "SFP+",
            "--speed", "25",
            "--count", "2");

        await ExecuteAsync(
            "switches", "port", "add", "sw03",
            "--type", "SFP+",
            "--speed", "25",
            "--count", "2");

        await ExecuteAsync(
            "connections", "add",
            "sw01", "0", "0",
            "sw02", "0", "0",
            "--label", "sw01-to-sw02");

        await ExecuteAsync(
            "connections", "add",
            "sw01", "0", "1",
            "sw03", "0", "0",
            "--label", "sw01-to-sw03");

        (var output, var yaml) = await ExecuteAsync("switches", "del", "sw01");

        Assert.Contains("Switch 'sw01' deleted.", output);
        Assert.Contains("sw02", yaml);
        Assert.Contains("sw03", yaml);
        // Both connections referencing sw01 should be removed
        Assert.DoesNotContain("sw01-to-sw02", yaml);
        Assert.DoesNotContain("sw01-to-sw03", yaml);
    }

    [Fact]
    public async Task deleting_resource_removes_connection_when_both_endpoints_referenced() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        await ExecuteAsync("servers", "add", "srv01");
        await ExecuteAsync("servers", "add", "srv02");

        await ExecuteAsync(
            "servers", "nic", "add", "srv01",
            "--type", "RJ45",
            "--speed", "10",
            "--ports", "2");

        await ExecuteAsync(
            "servers", "nic", "add", "srv02",
            "--type", "RJ45",
            "--speed", "10",
            "--ports", "2");

        await ExecuteAsync(
            "connections", "add",
            "srv01", "0", "0",
            "srv02", "0", "0",
            "--label", "bi-directional-link");

        await ExecuteAsync("servers", "del", "srv01");

        var yaml = await File.ReadAllTextAsync(Path.Combine(fs.Root, "config.yaml"));
        // srv02 should remain
        Assert.Contains("srv02", yaml);
        // Connection referencing deleted resource should be removed
        Assert.DoesNotContain("srv01", yaml);
        // Connection label should be gone since the connection is removed
        Assert.DoesNotContain("bi-directional-link", yaml);
    }
}
