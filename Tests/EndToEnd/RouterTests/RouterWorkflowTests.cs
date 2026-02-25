using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.RouterTests;

[Collection("Yaml CLI tests")]
public class RouterWorkflowTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
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
    public async Task routers_cli_workflow_test()
    {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Add router
        var (output, yaml) = await ExecuteAsync("routers", "add", "rt01");
        Assert.Equal("Router 'rt01' added.\n", output);
        Assert.Contains("name: rt01", yaml);

        // Update router
        (output, yaml) = await ExecuteAsync(
            "routers", "set", "rt01",
            "--Model", "Ubiquiti EdgeRouter 4",
            "--managed", "true",
            "--poe", "false"
        );
        Assert.Equal("Router 'rt01' updated.\n", output);

        Assert.Equal("""
                     version: 2
                     resources:
                     - kind: Router
                       model: Ubiquiti EdgeRouter 4
                       managed: true
                       poe: false
                       name: rt01

                     """, yaml);

        // Add second router
        (output, yaml) = await ExecuteAsync("routers", "add", "rt02");
        Assert.Equal("Router 'rt02' added.\n", output);

        (output, yaml) = await ExecuteAsync(
            "routers", "set", "rt02",
            "--Model", "TP-Link ER605",
            "--managed", "false",
            "--poe", "false"
        );
        Assert.Equal("Router 'rt02' updated.\n", output);

        Assert.Equal("""
                     version: 2
                     resources:
                     - kind: Router
                       model: Ubiquiti EdgeRouter 4
                       managed: true
                       poe: false
                       name: rt01
                     - kind: Router
                       model: TP-Link ER605
                       managed: false
                       poe: false
                       name: rt02

                     """, yaml);

        // Get router
        (output, yaml) = await ExecuteAsync("routers", "get", "rt01");
        Assert.Equal("rt01  Model: Ubiquiti EdgeRouter 4, Managed: Yes, PoE: No\n", output);

        // List routers (strict table)
        (output, yaml) = await ExecuteAsync("routers", "list");
        Assert.Equal("""
                     ╭──────┬───────────────────────┬─────────┬─────┬───────┬──────────────╮
                     │ Name │ Model                 │ Managed │ PoE │ Ports │ Port Summary │
                     ├──────┼───────────────────────┼─────────┼─────┼───────┼──────────────┤
                     │ rt01 │ Ubiquiti EdgeRouter 4 │ yes     │ no  │ 0     │ Unknown      │
                     │ rt02 │ TP-Link ER605         │ no      │ no  │ 0     │ Unknown      │
                     ╰──────┴───────────────────────┴─────────┴─────┴───────┴──────────────╯

                     """, output);

        // Summary
        (output, yaml) = await ExecuteAsync("routers", "summary");
        Assert.Contains("rt01", output);
        Assert.Contains("rt02", output);

        // Delete router
        (output, yaml) = await ExecuteAsync("routers", "del", "rt02");
        Assert.Equal("""
                     Router 'rt02' deleted.

                     """, output);

        // List again
        (output, yaml) = await ExecuteAsync("routers", "list");
        Assert.Equal("""
                     ╭──────┬───────────────────────┬─────────┬─────┬───────┬──────────────╮
                     │ Name │ Model                 │ Managed │ PoE │ Ports │ Port Summary │
                     ├──────┼───────────────────────┼─────────┼─────┼───────┼──────────────┤
                     │ rt01 │ Ubiquiti EdgeRouter 4 │ yes     │ no  │ 0     │ Unknown      │
                     ╰──────┴───────────────────────┴─────────┴─────┴───────┴──────────────╯

                     """, output);
    }
}
