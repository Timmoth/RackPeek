using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.ConnectionTests;

[Collection("Process-wide static state")]
public class RenameResourceTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
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
    public async Task rename_server_with_single_connection_preserves_connection() {
        await ExecuteAsync("servers", "add", "srv01");
        await ExecuteAsync("servers", "add", "srv02");

        await ExecuteAsync("servers", "nic", "add", "srv01",
            "--type", "RJ45", "--speed", "10", "--ports", "2");

        await ExecuteAsync("servers", "nic", "add", "srv02",
            "--type", "RJ45", "--speed", "10", "--ports", "2");

        await ExecuteAsync("connections", "add",
            "srv01", "0", "0",
            "srv02", "0", "0",
            "--label", "uplink-test");

        await ExecuteAsync("servers", "rename", "srv01", "srv01-renamed");

        (_, var yaml) = await ExecuteAsync("servers", "get", "srv01-renamed");

        Assert.Contains("name: srv01-renamed", yaml);
        Assert.Contains("srv01-renamed", yaml);
        Assert.Contains("srv02", yaml);
        Assert.Contains("uplink-test", yaml);
    }

    [Fact]
    public async Task rename_server_with_multiple_connections_preserves_all() {
        await ExecuteAsync("servers", "add", "srv01");
        await ExecuteAsync("servers", "add", "srv02");
        await ExecuteAsync("servers", "add", "srv03");
        await ExecuteAsync("servers", "add", "srv04");

        foreach (var s in new[] { "srv01", "srv02", "srv03", "srv04" }) {
            await ExecuteAsync("servers", "nic", "add", s,
                "--type", "RJ45", "--speed", "10", "--ports", "2");
        }

        await ExecuteAsync("connections", "add",
            "srv01", "0", "0",
            "srv02", "0", "0",
            "--label", "conn-to-srv02");

        await ExecuteAsync("connections", "add",
            "srv01", "0", "1",
            "srv03", "0", "0",
            "--label", "conn-to-srv03");

        await ExecuteAsync("connections", "add",
            "srv02", "0", "1",   // changed
            "srv04", "0", "1",
            "--label", "conn-to-srv04");

        await ExecuteAsync("servers", "rename", "srv01", "srv01-updated");

        var yaml = await File.ReadAllTextAsync(Path.Combine(fs.Root, "config.yaml"));

        Assert.Contains("name: srv01-updated", yaml);
        Assert.Contains("srv01-updated", yaml);
        Assert.Contains("srv02", yaml);
        Assert.Contains("srv03", yaml);
        Assert.Contains("srv04", yaml);
        Assert.Contains("conn-to-srv02", yaml);
        Assert.Contains("conn-to-srv03", yaml);
        Assert.Contains("conn-to-srv04", yaml);
    }

    [Fact]
    public async Task rename_both_connection_endpoints_preserves_connection() {
        await ExecuteAsync("servers", "add", "srv01");
        await ExecuteAsync("servers", "add", "srv02");

        await ExecuteAsync("servers", "nic", "add", "srv01",
            "--type", "RJ45", "--speed", "10", "--ports", "2");

        await ExecuteAsync("servers", "nic", "add", "srv02",
            "--type", "RJ45", "--speed", "10", "--ports", "2");

        await ExecuteAsync("connections", "add",
            "srv01", "0", "0",
            "srv02", "0", "0",
            "--label", "bi-directional-link");

        await ExecuteAsync("servers", "rename", "srv01", "new-srv01");
        await ExecuteAsync("servers", "rename", "srv02", "new-srv02");

        (_, var yaml) = await ExecuteAsync("servers", "get", "new-srv01");

        Assert.Contains("name: new-srv01", yaml);
        Assert.Contains("new-srv01", yaml);
        Assert.Contains("new-srv02", yaml);
        Assert.Contains("bi-directional-link", yaml);
    }

    [Fact]
    public async Task rename_switch_with_connections_preserves_connections() {
        await ExecuteAsync("switches", "add", "sw01");
        await ExecuteAsync("switches", "add", "sw02");

        await ExecuteAsync("switches", "port", "add", "sw01",
            "--type", "SFP+", "--speed", "25", "--count", "2");

        await ExecuteAsync("switches", "port", "add", "sw02",
            "--type", "SFP+", "--speed", "25", "--count", "2");

        await ExecuteAsync("connections", "add",
            "sw01", "0", "0",
            "sw02", "0", "0",
            "--label", "switch-uplink");

        await ExecuteAsync("switches", "rename", "sw01", "sw01-core");

        (_, var yaml) = await ExecuteAsync("switches", "get", "sw01-core");

        Assert.Contains("name: sw01-core", yaml);
        Assert.Contains("sw01-core", yaml);
        Assert.Contains("sw02", yaml);
        Assert.Contains("switch-uplink", yaml);
    }

    [Fact]
    public async Task rename_typed_in_different_case_preserves_connection() {
        await ExecuteAsync("servers", "add", "Case01");
        await ExecuteAsync("servers", "add", "Case02");

        await ExecuteAsync("servers", "nic", "add", "Case01",
            "--type", "RJ45", "--speed", "10", "--ports", "2");

        await ExecuteAsync("servers", "nic", "add", "Case02",
            "--type", "RJ45", "--speed", "10", "--ports", "2");

        await ExecuteAsync("connections", "add",
            "Case01", "0", "0",
            "Case02", "0", "0",
            "--label", "mixed-case-link");

        await ExecuteAsync("servers", "rename", "case01", "Case01-renamed");

        (_, var yaml) = await ExecuteAsync("servers", "get", "Case01-renamed");

        Assert.Contains("name: Case01-renamed", yaml);
        Assert.Contains("mixed-case-link", yaml);
        // The connection endpoint should follow the rename, not keep pointing at the old name
        Assert.Contains("resource: Case01-renamed", yaml);
    }

    [Fact]
    public async Task rename_typed_in_different_case_updates_runs_on() {
        await ExecuteAsync("servers", "add", "Case11");
        await ExecuteAsync("systems", "add", "sys-case-11");
        await ExecuteAsync("systems", "set", "sys-case-11", "--runs-on", "Case11");

        await ExecuteAsync("servers", "rename", "case11", "Case12");

        (_, var yaml) = await ExecuteAsync("servers", "get", "Case12");

        Assert.Contains("name: Case12", yaml);
        // The dependant system should follow the rename
        Assert.Contains("- Case12", yaml);
    }

    [Fact]
    public async Task rename_with_special_naming_preserves_connections() {
        await ExecuteAsync("servers", "add", "srv-prod-web-01");
        await ExecuteAsync("servers", "add", "srv-prod-app-01");

        await ExecuteAsync("servers", "nic", "add", "srv-prod-web-01",
            "--type", "RJ45", "--speed", "10", "--ports", "2");

        await ExecuteAsync("servers", "nic", "add", "srv-prod-app-01",
            "--type", "RJ45", "--speed", "10", "--ports", "2");

        await ExecuteAsync("connections", "add",
            "srv-prod-web-01", "0", "0",
            "srv-prod-app-01", "0", "0",
            "--label", "app-backend-link");

        await ExecuteAsync("servers", "rename", "srv-prod-web-01", "srv_prod_web_01");

        (_, var yaml) = await ExecuteAsync("servers", "get", "srv_prod_web_01");

        Assert.Contains("name: srv_prod_web_01", yaml);
        Assert.Contains("srv_prod_web_01", yaml);
        Assert.Contains("srv-prod-app-01", yaml);
        Assert.Contains("app-backend-link", yaml);
    }
}
