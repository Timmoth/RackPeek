using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.FirewallTests;

[Collection("Yaml CLI tests")]
public class FirewallWorkflowTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture>
{
    private async Task<(string, string)> ExecuteAsync(params string[] args)
    {
        outputHelper.WriteLine($"rpk {string.Join(" ", args)}");

        var output = await YamlCliTestHost.RunAsync(
            args,
            fs.Root,
            outputHelper,
            "config.yaml"
        );

        outputHelper.WriteLine(output);

        var yaml = await File.ReadAllTextAsync(Path.Combine(fs.Root, "config.yaml"));
        return (output, yaml);
    }

    [Fact]
    public async Task firewalls_cli_workflow_test()
    {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Add firewall
        var (output, yaml) = await ExecuteAsync("firewalls", "add", "fw01");
        Assert.Equal("Firewall 'fw01' added.\n", output);
        Assert.Contains("name: fw01", yaml);

        // Update firewall
        (output, yaml) = await ExecuteAsync(
            "firewalls", "set", "fw01",
            "--Model", "Fortinet FG-60F",
            "--managed", "true",
            "--poe", "false"
        );
        Assert.Equal("Firewall 'fw01' updated.\n", output);

        Assert.Equal("""
                     version: 1
                     resources:
                     - kind: Firewall
                       model: Fortinet FG-60F
                       managed: true
                       poe: false
                       name: fw01

                     """, yaml);

        // Add second firewall
        (output, yaml) = await ExecuteAsync("firewalls", "add", "fw02");
        Assert.Equal("Firewall 'fw02' added.\n", output);

        (output, yaml) = await ExecuteAsync(
            "firewalls", "set", "fw02",
            "--Model", "Ubiquiti UXG-Lite",
            "--managed", "false",
            "--poe", "false"
        );
        Assert.Equal("Firewall 'fw02' updated.\n", output);

        Assert.Equal("""
                     version: 1
                     resources:
                     - kind: Firewall
                       model: Fortinet FG-60F
                       managed: true
                       poe: false
                       name: fw01
                     - kind: Firewall
                       model: Ubiquiti UXG-Lite
                       managed: false
                       poe: false
                       name: fw02

                     """, yaml);

        // Get firewall
        (output, yaml) = await ExecuteAsync("firewalls", "get", "fw01");
        Assert.Equal("fw01  Model: Fortinet FG-60F, Managed: Yes, PoE: No\n", output);

        // List firewalls
        (output, yaml) = await ExecuteAsync("firewalls", "list");
        Assert.Equal("""
                     ╭──────┬───────────────────┬─────────┬─────┬───────┬──────────────╮
                     │ Name │ Model             │ Managed │ PoE │ Ports │ Port Summary │
                     ├──────┼───────────────────┼─────────┼─────┼───────┼──────────────┤
                     │ fw01 │ Fortinet FG-60F   │ yes     │ no  │ 0     │ Unknown      │
                     │ fw02 │ Ubiquiti UXG-Lite │ no      │ no  │ 0     │ Unknown      │
                     ╰──────┴───────────────────┴─────────┴─────┴───────┴──────────────╯

                     """, output);

        // Summary
        (output, yaml) = await ExecuteAsync("firewalls", "summary");
        Assert.Equal("""
                     ╭──────┬───────────────────┬─────────┬─────┬───────┬───────────┬──────────────╮
                     │ Name │ Model             │ Managed │ PoE │ Ports │ Max Speed │ Port Summary │
                     ├──────┼───────────────────┼─────────┼─────┼───────┼───────────┼──────────────┤
                     │ fw01 │ Fortinet FG-60F   │ yes     │ no  │ 0     │ 0G        │ Unknown      │
                     │ fw02 │ Ubiquiti UXG-Lite │ no      │ no  │ 0     │ 0G        │ Unknown      │
                     ╰──────┴───────────────────┴─────────┴─────┴───────┴───────────┴──────────────╯

                     """, output);

        // Delete firewall
        (output, yaml) = await ExecuteAsync("firewalls", "del", "fw02");
        Assert.Equal("""
                     Firewall 'fw02' deleted.

                     """, output);

        // List again
        (output, yaml) = await ExecuteAsync("firewalls", "list");
        Assert.Equal("""
                     ╭──────┬─────────────────┬─────────┬─────┬───────┬──────────────╮
                     │ Name │ Model           │ Managed │ PoE │ Ports │ Port Summary │
                     ├──────┼─────────────────┼─────────┼─────┼───────┼──────────────┤
                     │ fw01 │ Fortinet FG-60F │ yes     │ no  │ 0     │ Unknown      │
                     ╰──────┴─────────────────┴─────────┴─────┴───────┴──────────────╯

                     """, output);
    }
}
