using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.ServerTests;

[Collection("Process-wide static state")]
public class ServerCommandTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture> {
    private async Task<(string output, string yaml)> ExecuteAsync(params string[] args) {
        var output = await YamlCliTestHost.RunAsync(
            args,
            fs.Root,
            outputHelper,
            "config.yaml");

        var yaml = await File.ReadAllTextAsync(Path.Combine(fs.Root, "config.yaml"));
        return (output, yaml);
    }

    [Fact]
    public async Task describe_outputs_expected_information() {
        await ExecuteAsync("servers", "add", "srv01");
        await ExecuteAsync("servers", "set", "srv01", "--ram", "64");

        (var output, var _) = await ExecuteAsync("servers", "describe", "srv01");

        Assert.Contains("srv01", output);
        Assert.Contains("64", output);
    }

    [Fact]
    public async Task help_commands_do_not_throw() {
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
        Assert.Contains("Rename a server", (await ExecuteAsync("servers", "rename", "--help")).output);
    }

    [Fact]
    public async Task rename_successfully_updates_name() {
        await ExecuteAsync("servers", "add", "srv01");
        await ExecuteAsync("servers", "set", "srv01", "--ram", "64");

        (var output, var yaml) = await ExecuteAsync("servers", "rename", "srv01", "srv01-new");

        Assert.Equal("Server 'srv01' renamed to 'srv01-new'.\n", output);
        Assert.Contains("name: srv01-new", yaml);
    }

    [Fact]
    public async Task rename_updates_dependants() {
        await ExecuteAsync("servers", "add", "srv01");
        await ExecuteAsync("systems", "add", "sys01", "--runs-on", "srv01");

        (var output, var yaml) = await ExecuteAsync("servers", "rename", "srv01", "srv01-updated");

        (_, yaml) = await ExecuteAsync("systems", "get", "sys01");

        Assert.Contains("srv01-updated", yaml);
        Assert.DoesNotContain("srv01\n", yaml);
    }
}
