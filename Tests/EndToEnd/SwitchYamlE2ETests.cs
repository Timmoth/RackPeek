using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd;

[Collection("Yaml CLI tests")]
public class SwitchYamlE2ETests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture>
{
    private async Task<(string, string)> ExecuteAsync(params string[] args)
    {
        outputHelper.WriteLine($"rpk {string.Join(" ", args)}");

        var inputArgs = args.ToArray();
        var output = await YamlCliTestHost.RunAsync(
            inputArgs,
            fs.Root,
            outputHelper);

        outputHelper.WriteLine(output);

        var yaml = await File.ReadAllTextAsync(Path.Combine(fs.Root, "config.yaml"));
        return (output, yaml);
    }

    [Fact]
    public async Task switches_cli_workflow_test()
    {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Add switch
        var (output, yaml) = await ExecuteAsync("switches", "add", "sw01");
        Assert.Equal("Switch 'sw01' added.\n", output);
        Assert.Contains("name: sw01", yaml);

        (output, yaml) = await ExecuteAsync("switches", "set", "sw01", "--Model", "Netgear GS108", "--managed", "true",
            "--poe", "true");
        Assert.Equal("Server 'sw01' updated.\n", output);
        Assert.Equal("""
                     resources:
                     - kind: Switch
                       model: Netgear GS108
                       managed: true
                       poe: true
                       ports: 
                       name: sw01
                       tags: 

                     """, yaml);

        (output, yaml) = await ExecuteAsync("switches", "add", "sw02");
        Assert.Equal("Switch 'sw02' added.\n", output);
        Assert.Contains("name: sw02", yaml);

        (output, yaml) = await ExecuteAsync("switches", "set", "sw02", "--Model", "TP-Link TL-SG108E", "--managed",
            "false", "--poe", "false");
        Assert.Equal("Server 'sw02' updated.\n", output);

        Assert.Equal("""
                     resources:
                     - kind: Switch
                       model: Netgear GS108
                       managed: true
                       poe: true
                       ports: 
                       name: sw01
                       tags: 
                     - kind: Switch
                       model: TP-Link TL-SG108E
                       managed: false
                       poe: false
                       ports: 
                       name: sw02
                       tags: 

                     """, yaml);

        (output, yaml) = await ExecuteAsync("switches", "get", "sw01");
        Assert.Equal("sw01  Model: Netgear GS108, Managed: Yes, PoE: Yes\n", output);


        (output, yaml) = await ExecuteAsync("switches", "list");
        Assert.Equal("""
                     ╭──────┬───────────────────┬─────────┬─────┬───────┬──────────────╮
                     │ Name │ Model             │ Managed │ PoE │ Ports │ Port Summary │
                     ├──────┼───────────────────┼─────────┼─────┼───────┼──────────────┤
                     │ sw01 │ Netgear GS108     │ yes     │ yes │ 0     │ Unknown      │
                     │ sw02 │ TP-Link TL-SG108E │ no      │ no  │ 0     │ Unknown      │
                     ╰──────┴───────────────────┴─────────┴─────┴───────┴──────────────╯

                     """, output);

        (output, yaml) = await ExecuteAsync("switches", "summary");
        Assert.Contains("""
                        ╭──────┬───────────────────┬─────────┬─────┬───────┬───────────┬──────────────╮
                        │ Name │ Model             │ Managed │ PoE │ Ports │ Max Speed │ Port Summary │
                        ├──────┼───────────────────┼─────────┼─────┼───────┼───────────┼──────────────┤
                        │ sw01 │ Netgear GS108     │ yes     │ yes │ 0     │ 0G        │ Unknown      │
                        │ sw02 │ TP-Link TL-SG108E │ no      │ no  │ 0     │ 0G        │ Unknown      │
                        ╰──────┴───────────────────┴─────────┴─────┴───────┴───────────┴──────────────╯

                        """, output);

        (output, yaml) = await ExecuteAsync("switches", "del", "sw02");
        Assert.Equal("""
                     Switch 'sw02' deleted.

                     """, output);

        (output, yaml) = await ExecuteAsync("switches", "list");
        Assert.Equal("""
                     ╭──────┬───────────────┬─────────┬─────┬───────┬──────────────╮
                     │ Name │ Model         │ Managed │ PoE │ Ports │ Port Summary │
                     ├──────┼───────────────┼─────────┼─────┼───────┼──────────────┤
                     │ sw01 │ Netgear GS108 │ yes     │ yes │ 0     │ Unknown      │
                     ╰──────┴───────────────┴─────────┴─────┴───────┴──────────────╯

                     """, output);
    }
}