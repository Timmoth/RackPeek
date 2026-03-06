using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.RouterTests;

[Collection("Yaml CLI tests")]
public class RouterCommandTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
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
        await ExecuteAsync("routers", "add", "rt01");
        await ExecuteAsync("routers", "set", "rt01", "--Model", "Ubiquiti EdgeRouter 4");

        var (output, _) = await ExecuteAsync("routers", "describe", "rt01");

        Assert.Contains("rt01", output);
        Assert.Contains("Ubiquiti EdgeRouter 4", output);
        Assert.Contains("Managed", output);
        Assert.Contains("PoE", output);
    }

    [Fact]
    public async Task help_commands_do_not_throw()
    {
        Assert.Contains("Manage network routers", (await ExecuteAsync("routers", "--help")).Item1);
        Assert.Contains("Add a new network router", (await ExecuteAsync("routers", "add", "--help")).Item1);
        Assert.Contains("List all routers", (await ExecuteAsync("routers", "list", "--help")).Item1);
        Assert.Contains("Retrieve details", (await ExecuteAsync("routers", "get", "--help")).Item1);
        Assert.Contains("Show detailed information", (await ExecuteAsync("routers", "describe", "--help")).Item1);
        Assert.Contains("Update properties", (await ExecuteAsync("routers", "set", "--help")).Item1);
        Assert.Contains("Delete a router", (await ExecuteAsync("routers", "del", "--help")).Item1);

        // Port help
        Assert.Contains("Manage ports", (await ExecuteAsync("routers", "port", "--help")).Item1);
        Assert.Contains("Add a port", (await ExecuteAsync("routers", "port", "add", "--help")).Item1);
        Assert.Contains("Update a router port", (await ExecuteAsync("routers", "port", "set", "--help")).Item1);
        Assert.Contains("Remove a port", (await ExecuteAsync("routers", "port", "del", "--help")).Item1);
    }
}
