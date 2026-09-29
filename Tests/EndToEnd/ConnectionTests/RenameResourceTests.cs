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

    [Fact]
    public async Task rename_that_only_changes_case_updates_the_name() {
        await ExecuteAsync("servers", "add", "CaseOnly21");

        (var output, var yaml) = await ExecuteAsync("servers", "rename", "CaseOnly21", "caseonly21");

        Assert.Contains("Server 'CaseOnly21' renamed to 'caseonly21'.", output);
        Assert.Contains("name: caseonly21", yaml);
        Assert.DoesNotContain("name: CaseOnly21", yaml);
    }

    [Fact]
    public async Task rename_that_only_changes_case_updates_runs_on_and_connections() {
        await ExecuteAsync("servers", "add", "CaseOnly31");
        await ExecuteAsync("servers", "add", "CaseOnly32");

        await ExecuteAsync("servers", "nic", "add", "CaseOnly31",
            "--type", "RJ45", "--speed", "10", "--ports", "2");

        await ExecuteAsync("servers", "nic", "add", "CaseOnly32",
            "--type", "RJ45", "--speed", "10", "--ports", "2");

        await ExecuteAsync("connections", "add",
            "CaseOnly31", "0", "0",
            "CaseOnly32", "0", "0",
            "--label", "case-only-link");

        await ExecuteAsync("systems", "add", "sys-case-only-31");
        await ExecuteAsync("systems", "set", "sys-case-only-31", "--runs-on", "CaseOnly31");

        (var output, var yaml) = await ExecuteAsync("servers", "rename", "CaseOnly31", "CASEONLY31");

        Assert.Contains("Server 'CaseOnly31' renamed to 'CASEONLY31'.", output);
        Assert.Contains("name: CASEONLY31", yaml);
        Assert.Contains("resource: CASEONLY31", yaml);
        Assert.Contains("- CASEONLY31", yaml);
        Assert.DoesNotContain("CaseOnly31", yaml);
    }

    [Fact]
    public async Task rename_onto_another_resources_case_variant_is_still_refused() {
        await ExecuteAsync("servers", "add", "Clash41");
        await ExecuteAsync("servers", "add", "Clash42");
        await ExecuteAsync("systems", "add", "clash43");

        (var sameKind, _) = await ExecuteAsync("servers", "rename", "Clash41", "CLASH42");
        (var otherKind, var yaml) = await ExecuteAsync("servers", "rename", "Clash41", "CLASH43");

        Assert.Contains("Conflict: Server resource 'CLASH42' already exists.", sameKind);
        Assert.Contains("Conflict: System resource 'CLASH43' already exists.", otherKind);
        Assert.Contains("name: Clash41", yaml);
        Assert.Contains("name: Clash42", yaml);
        Assert.Contains("name: clash43", yaml);
        Assert.DoesNotContain("CLASH4", yaml);
    }

    [Fact]
    public async Task rename_to_the_identical_name_leaves_the_file_unchanged() {
        await ExecuteAsync("servers", "add", "Same51");
        await ExecuteAsync("servers", "add", "Same52");
        await ExecuteAsync("servers", "add", "Same53");

        foreach (var s in new[] { "Same51", "Same52", "Same53" }) {
            await ExecuteAsync("servers", "nic", "add", s,
                "--type", "RJ45", "--speed", "10", "--ports", "2");
        }

        await ExecuteAsync("connections", "add",
            "Same51", "0", "0",
            "Same52", "0", "0",
            "--label", "same-link");

        await ExecuteAsync("connections", "add",
            "Same52", "0", "1",
            "Same53", "0", "0",
            "--label", "other-link");

        await ExecuteAsync("systems", "add", "sys-same-51");
        (_, var before) = await ExecuteAsync("systems", "set", "sys-same-51", "--runs-on", "same51");

        (var output, var after) = await ExecuteAsync("servers", "rename", "Same51", "Same51");

        Assert.Contains("Server 'Same51' renamed to 'Same51'.", output);
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task rename_is_refused_when_the_config_already_holds_a_case_variant() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
            version: 4
            resources:
            - kind: Server
              name: Dup61
            - kind: Server
              name: dup61
            connections: []
            """);

        (var output, var yaml) = await ExecuteAsync("servers", "rename", "Dup61", "DUP61");

        Assert.Contains("Conflict: Server resource 'DUP61' already exists.", output);
        Assert.Contains("name: Dup61", yaml);
        Assert.Contains("name: dup61", yaml);
        Assert.DoesNotContain("DUP61", yaml);
    }
}
