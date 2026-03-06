using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.ServiceTests;

[Collection("Yaml CLI tests")]
public class ServiceCommandTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
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
        await ExecuteAsync("services", "add", "svc01");
        // ToDo Introduce CIDR validation and enforce it in the test 
        await ExecuteAsync("services", "set", "svc01", "--ip", "1.2.3.4");

        var (output, _) = await ExecuteAsync("services", "describe", "svc01");

        Assert.Contains("svc01", output);
        Assert.Contains("1.2.3.4", output);
    }

    [Fact]
    public async Task help_commands_do_not_throw()
    {
        Assert.Contains("Manage services", (await ExecuteAsync("services", "--help")).output);
        Assert.Contains("Add a new service", (await ExecuteAsync("services", "add", "--help")).output);
        Assert.Contains("List all services", (await ExecuteAsync("services", "list", "--help")).output);
        Assert.Contains("Retrieve a service", (await ExecuteAsync("services", "get", "--help")).output);
        Assert.Contains("Show detailed information", (await ExecuteAsync("services", "describe", "--help")).output);
        Assert.Contains("Update properties", (await ExecuteAsync("services", "set", "--help")).output);
        Assert.Contains("Delete a service", (await ExecuteAsync("services", "del", "--help")).output);
        Assert.Contains("List subnets", (await ExecuteAsync("services", "subnets", "--help")).output);
    }
}
