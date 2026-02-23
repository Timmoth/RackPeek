using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.FirewallTests;

[Collection("Yaml CLI tests")]
public class FirewallCommandTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
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
        await ExecuteAsync("firewalls", "add", "fw01");
        await ExecuteAsync("firewalls", "set", "fw01", "--Model", "Fortinet FG-60F", "--managed", "true", "--poe", "false");

        var (output, _) = await ExecuteAsync("firewalls", "describe", "fw01");

        Assert.Contains("fw01", output);
        Assert.Contains("Fortinet FG-60F", output);
        Assert.Contains("Managed", output);
        Assert.Contains("PoE", output);
    }

    [Fact]
    public async Task help_commands_do_not_throw()
    {
        Assert.Contains("Manage firewalls", (await ExecuteAsync("firewalls", "--help")).Item1);
        Assert.Contains("Add a new firewall", (await ExecuteAsync("firewalls", "add", "--help")).Item1);
        Assert.Contains("List all firewalls", (await ExecuteAsync("firewalls", "list", "--help")).Item1);
        Assert.Contains("Retrieve details", (await ExecuteAsync("firewalls", "get", "--help")).Item1);
        Assert.Contains("Show detailed information", (await ExecuteAsync("firewalls", "describe", "--help")).Item1);
        Assert.Contains("Update properties", (await ExecuteAsync("firewalls", "set", "--help")).Item1);
        Assert.Contains("Delete a firewall", (await ExecuteAsync("firewalls", "del", "--help")).Item1);

        // Port help
        Assert.Contains("Manage ports", (await ExecuteAsync("firewalls", "port", "--help")).Item1);
        Assert.Contains("Add a port", (await ExecuteAsync("firewalls", "port", "add", "--help")).Item1);
        Assert.Contains("Update a firewall port", (await ExecuteAsync("firewalls", "port", "set", "--help")).Item1);
        Assert.Contains("Remove a port", (await ExecuteAsync("firewalls", "port", "del", "--help")).Item1);
    }
}
