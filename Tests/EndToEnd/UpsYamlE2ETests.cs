using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd;

[Collection("Yaml CLI tests")]
public class UpsYamlE2ETests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture>
{
    private async Task<(string, string)> ExecuteAsync(params string[] args)
    {
        outputHelper.WriteLine($"rpk {string.Join(" ", args)}");

        var inputArgs = args.ToArray();
        var output = await YamlCliTestHost.RunAsync(
            inputArgs,
            fs.Root,
            outputHelper,
            "config.yaml");

        outputHelper.WriteLine(output);

        var yaml = await File.ReadAllTextAsync(Path.Combine(fs.Root, "config.yaml"));
        return (output, yaml);
    }

    [Fact]
    public async Task ups_cli_workflow_test()
    {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Add UPS
        var (output, yaml) = await ExecuteAsync("ups", "add", "ups01");
        Assert.Equal("UPS 'ups01' added.\n", output);
        Assert.Contains("name: ups01", yaml);

        // Update UPS
        (output, yaml) = await ExecuteAsync(
            "ups", "set", "ups01",
            "--model", "APC Smart-UPS 1500",
            "--va", "1500"
        );
        Assert.Equal("UPS 'ups01' updated.\n", output);

        Assert.Equal("""
                     resources:
                     - kind: Ups
                       model: APC Smart-UPS 1500
                       va: 1500
                       name: ups01

                     """, yaml);

        // Add second UPS
        (output, yaml) = await ExecuteAsync("ups", "add", "ups02");
        Assert.Equal("UPS 'ups02' added.\n", output);
        Assert.Contains("name: ups02", yaml);

        (output, yaml) = await ExecuteAsync(
            "ups", "set", "ups02",
            "--model", "CyberPower CP1500PFCLCD",
            "--va", "1500"
        );
        Assert.Equal("UPS 'ups02' updated.\n", output);

        Assert.Equal("""
                     resources:
                     - kind: Ups
                       model: APC Smart-UPS 1500
                       va: 1500
                       name: ups01
                     - kind: Ups
                       model: CyberPower CP1500PFCLCD
                       va: 1500
                       name: ups02

                     """, yaml);

        // Get UPS
        (output, yaml) = await ExecuteAsync("ups", "get", "ups01");
        Assert.Equal("ups01  Model: APC Smart-UPS 1500, VA: 1500\n", output);

        // List UPS units
        (output, yaml) = await ExecuteAsync("ups", "list");
        Assert.Equal("""
                     ╭───────┬─────────────────────────┬──────╮
                     │ Name  │ Model                   │ VA   │
                     ├───────┼─────────────────────────┼──────┤
                     │ ups01 │ APC Smart-UPS 1500      │ 1500 │
                     │ ups02 │ CyberPower CP1500PFCLCD │ 1500 │
                     ╰───────┴─────────────────────────┴──────╯

                     """, output);

        // Summary
        (output, yaml) = await ExecuteAsync("ups", "summary");
        Assert.Contains("""
                        ╭───────┬─────────────────────────┬──────╮
                        │ Name  │ Model                   │ VA   │
                        ├───────┼─────────────────────────┼──────┤
                        │ ups01 │ APC Smart-UPS 1500      │ 1500 │
                        │ ups02 │ CyberPower CP1500PFCLCD │ 1500 │
                        ╰───────┴─────────────────────────┴──────╯

                        """, output);

        // Delete UPS
        (output, yaml) = await ExecuteAsync("ups", "del", "ups02");
        Assert.Equal("""
                     UPS 'ups02' deleted.

                     """, output);

        // List again
        (output, yaml) = await ExecuteAsync("ups", "list");
        Assert.Equal("""
                     ╭───────┬────────────────────┬──────╮
                     │ Name  │ Model              │ VA   │
                     ├───────┼────────────────────┼──────┤
                     │ ups01 │ APC Smart-UPS 1500 │ 1500 │
                     ╰───────┴────────────────────┴──────╯

                     """, output);
    }
}