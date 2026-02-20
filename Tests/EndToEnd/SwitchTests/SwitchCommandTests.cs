using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd;

[Collection("Yaml CLI tests")]
public class SwitchCommandTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture>
{
    private async Task<(string, string)> ExecuteAsync(params string[] args)
    {
        var output = await YamlCliTestHost.RunAsync(
            args,
            fs.Root,
            outputHelper,
            "config.yaml"
        );

        var yaml = await File.ReadAllTextAsync(Path.Combine(fs.Root, "config.yaml"));
        return (output, yaml);
    }

    [Fact]
    public async Task describe_outputs_expected_information()
    {
        await ExecuteAsync("switches", "add", "sw01");
        await ExecuteAsync("switches", "set", "sw01", "--Model", "Netgear GS108", "--managed", "true", "--poe", "true");

        var (output, _) = await ExecuteAsync("switches", "describe", "sw01");

        Assert.Contains("sw01", output);
        Assert.Contains("Netgear GS108", output);
        Assert.Contains("Managed", output);
        Assert.Contains("PoE", output);
    }

    [Fact]
    public async Task help_commands_do_not_throw()
    {
        Assert.Contains("Manage network switches", (await ExecuteAsync("switches", "--help")).Item1);
        Assert.Contains("Add a new network switch", (await ExecuteAsync("switches", "add", "--help")).Item1);
        Assert.Contains("List all switches", (await ExecuteAsync("switches", "list", "--help")).Item1);
        Assert.Contains("Retrieve details", (await ExecuteAsync("switches", "get", "--help")).Item1);
        Assert.Contains("Show detailed information", (await ExecuteAsync("switches", "describe", "--help")).Item1);
        Assert.Contains("Update properties", (await ExecuteAsync("switches", "set", "--help")).Item1);
        Assert.Contains("Delete a switch", (await ExecuteAsync("switches", "del", "--help")).Item1);

        // Port help
        Assert.Contains("Manage ports", (await ExecuteAsync("switches", "port", "--help")).Item1);
        Assert.Contains("Add a port", (await ExecuteAsync("switches", "port", "add", "--help")).Item1);
        Assert.Contains("Update a switch port", (await ExecuteAsync("switches", "port", "set", "--help")).Item1);
        Assert.Contains("Remove a port", (await ExecuteAsync("switches", "port", "del", "--help")).Item1);
    }
}
