using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.SystemTests;

[Collection("Yaml CLI tests")]
public class SystemCommandTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
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
        await ExecuteAsync("systems", "add", "sys01");
        await ExecuteAsync("systems", "set", "sys01", "--type", "vm", "--os", "ubuntu", "--cores", "4", "--ram", "16");

        var (output, _) = await ExecuteAsync("systems", "describe", "sys01");

        Assert.Contains("sys01", output);
        Assert.Contains("vm", output);
        Assert.Contains("ubuntu", output);
        Assert.Contains("Cores", output);
        Assert.Contains("RAM", output);
    }

    [Fact]
    public async Task help_commands_do_not_throw()
    {
        Assert.Contains("Manage systems", (await ExecuteAsync("systems", "--help")).Item1);
        Assert.Contains("Add a new system", (await ExecuteAsync("systems", "add", "--help")).Item1);
        Assert.Contains("List all systems", (await ExecuteAsync("systems", "list", "--help")).Item1);
        Assert.Contains("Retrieve a system", (await ExecuteAsync("systems", "get", "--help")).Item1);
        Assert.Contains("Display detailed information", (await ExecuteAsync("systems", "describe", "--help")).Item1);
        Assert.Contains("Update properties", (await ExecuteAsync("systems", "set", "--help")).Item1);
        Assert.Contains("Delete a system", (await ExecuteAsync("systems", "del", "--help")).Item1);
        Assert.Contains("Display the dependency tree", (await ExecuteAsync("systems", "tree", "--help")).Item1);
    }
}
