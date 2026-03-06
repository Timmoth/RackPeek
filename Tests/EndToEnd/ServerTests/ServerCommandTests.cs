using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.ServerTests;

[Collection("Yaml CLI tests")]
public class ServerCommandTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture>
{
    private async Task<(string output, string yaml)> ExecuteAsync(params string[] args)
    {
        var output = await YamlCliTestHost.RunAsync(
            args,
            fs.Root,
            outputHelper,
            "config.yaml");

        var yaml = await File.ReadAllTextAsync(Path.Combine(fs.Root, "config.yaml"));
        return (output, yaml);
    }

    [Fact]
    public async Task describe_outputs_expected_information()
    {
        await ExecuteAsync("servers", "add", "srv01");
        await ExecuteAsync("servers", "set", "srv01", "--ram", "64");

        var (output, _) = await ExecuteAsync("servers", "describe", "srv01");

        Assert.Contains("srv01", output);
        Assert.Contains("64", output);
    }

    [Fact]
    public async Task help_commands_do_not_throw()
    {
        Assert.Contains("Manage servers", (await ExecuteAsync("servers", "--help")).output);
        Assert.Contains("Add a new server", (await ExecuteAsync("servers", "add", "--help")).output);
        Assert.Contains("List all servers", (await ExecuteAsync("servers", "get", "--help")).output);
        Assert.Contains("Display detailed information", (await ExecuteAsync("servers", "describe", "--help")).output);
        Assert.Contains("Update properties", (await ExecuteAsync("servers", "set", "--help")).output);
        Assert.Contains("Delete a server", (await ExecuteAsync("servers", "del", "--help")).output);
        Assert.Contains("Display the dependency tree", (await ExecuteAsync("servers", "tree", "--help")).output);

        Assert.Contains("Manage CPUs", (await ExecuteAsync("servers", "cpu", "--help")).output);
        Assert.Contains("Manage drives", (await ExecuteAsync("servers", "drive", "--help")).output);
        Assert.Contains("Manage GPUs", (await ExecuteAsync("servers", "gpu", "--help")).output);
        Assert.Contains("Manage network interface cards", (await ExecuteAsync("servers", "nic", "--help")).output);
    }
}
