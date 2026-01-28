using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd;

[Collection("Yaml CLI tests")]
public class ServerYamlE2ETests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture>
{
    private async Task<(string, string)> ExecuteAsync(params string[] args)
    {
        outputHelper.WriteLine($"rpk {string.Join(" ", args)}");

        var inputArgs = args.ToArray();
        var output = await YamlCliTestHost.RunAsync(
            inputArgs,
            fs.Root,
            outputHelper
        );

        outputHelper.WriteLine(output);

        var yaml = await File.ReadAllTextAsync(Path.Combine(fs.Root, "config.yaml"));
        return (output, yaml);
    }

    [Fact]
    public async Task servers_cli_workflow_test()
    {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Add switch
        var (output, yaml) = await ExecuteAsync("servers", "add", "node01");
        Assert.Equal("Server 'node01' added.\n", output);
        Assert.Contains("name: node01", yaml);
    }

    [Fact]
    public async Task servers_tree_cli_workflow_test()
    {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");
        // Add switch
        var (output, yaml) = await ExecuteAsync("servers", "add", "node01");
        Assert.Equal("Server 'node01' added.\n", output);
        Assert.Contains("name: node01", yaml);

        (output, yaml) = await ExecuteAsync("systems", "add", "host01");
        Assert.Equal("System 'host01' added.\n", output);

        (output, yaml) = await ExecuteAsync("systems", "add", "host02");
        Assert.Equal("System 'host02' added.\n", output);

        (output, yaml) = await ExecuteAsync("systems", "add", "host03");
        Assert.Equal("System 'host03' added.\n", output);

        (output, yaml) = await ExecuteAsync(
            "systems", "set", "host01",
            "--runs-on", "node01"
        );
        Assert.Equal("System 'host01' updated.\n", output);
        (output, yaml) = await ExecuteAsync(
            "systems", "set", "host02",
            "--runs-on", "node01"
        );
        Assert.Equal("System 'host02' updated.\n", output);
        (output, yaml) = await ExecuteAsync(
            "systems", "set", "host03",
            "--runs-on", "node01"
        );
        Assert.Equal("System 'host03' updated.\n", output);


        (output, yaml) = await ExecuteAsync("services", "add", "immich");
        Assert.Equal("Service 'immich' added.\n", output);

        (output, yaml) = await ExecuteAsync("services", "add", "paperless");
        Assert.Equal("Service 'paperless' added.\n", output);

        (output, yaml) = await ExecuteAsync(
            "services", "set", "immich",
            "--runs-on", "host01"
        );

        (output, yaml) = await ExecuteAsync(
            "services", "set", "paperless",
            "--runs-on", "host01"
        );

        (output, yaml) = await ExecuteAsync("servers", "tree", "node01");
        Assert.Equal("""
                     node01
                     ├── System: host01
                     │   ├── Service: immich
                     │   └── Service: paperless
                     ├── System: host02
                     └── System: host03

                     """, output);
    }
}