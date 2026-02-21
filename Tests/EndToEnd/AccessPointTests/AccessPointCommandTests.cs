using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.AccessPointTests;

[Collection("Yaml CLI tests")]
public class AccessPointCommandTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture>
{
    private async Task<(string, string)> ExecuteAsync(params string[] args)
    {
        var output = await YamlCliTestHost.RunAsync(args, fs.Root, outputHelper, "config.yaml");

        var yaml = await File.ReadAllTextAsync(Path.Combine(fs.Root, "config.yaml"));
        return (output, yaml);
    }

    [Fact]
    public async Task describe_returns_detailed_information()
    {
        // given
        await ExecuteAsync("accesspoints", "add", "ap01");
        await ExecuteAsync("accesspoints", "set", "ap01", "--model", "U6-Lite", "--speed", "1");

        // when
        var (output, _) = await ExecuteAsync("accesspoints", "describe", "ap01");

        // then
        Assert.Contains("Name:", output);
        Assert.Contains("ap01", output);
        Assert.Contains("Model:", output);
        Assert.Contains("U6-Lite", output);
        Assert.Contains("Speed (Gbps):", output);
        Assert.Contains("1", output);
    }

    [Fact]
    public async Task help_outputs_do_not_throw()
    {
        var (rootHelp, _) = await ExecuteAsync("accesspoints", "--help");
        Assert.Contains("Manage access points", rootHelp);

        var (addHelp, _) = await ExecuteAsync("accesspoints", "add", "--help");
        Assert.Contains("Add a new access point", addHelp);

        var (setHelp, _) = await ExecuteAsync("accesspoints", "set", "--help");
        Assert.Contains("Update properties", setHelp);

        var (describeHelp, _) = await ExecuteAsync("accesspoints", "describe", "--help");
        Assert.Contains("Show detailed information", describeHelp);
    }
}