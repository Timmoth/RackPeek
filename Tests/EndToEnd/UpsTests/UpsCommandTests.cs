using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.UpsTests;

[Collection("Yaml CLI tests")]
public class UpsCommandTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture>
{
    private async Task<(string, string)> ExecuteAsync(params string[] args)
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
    public async Task describe_returns_detailed_information()
    {
        // given
        await ExecuteAsync("ups", "add", "ups01");
        await ExecuteAsync("ups", "set", "ups01", "--model", "APC Smart-UPS 1500", "--va", "1500");

        // when
        var (output, _) = await ExecuteAsync("ups", "describe", "ups01");

        // then 
        Assert.Contains("Name:", output);
        Assert.Contains("ups01", output);

        Assert.Contains("Model:", output);
        Assert.Contains("APC Smart-UPS 1500", output);

        Assert.Contains("VA", output);
        Assert.Contains("1500", output);
    }

    [Fact]
    public async Task help_outputs_do_not_throw()
    {
        var (rootHelp, _) = await ExecuteAsync("ups", "--help");
        Assert.Contains("Manage UPS units", rootHelp);

        var (addHelp, _) = await ExecuteAsync("ups", "add", "--help");
        Assert.Contains("Add a new UPS unit", addHelp);

        var (setHelp, _) = await ExecuteAsync("ups", "set", "--help");
        Assert.Contains("Update properties", setHelp);

        var (describeHelp, _) = await ExecuteAsync("ups", "describe", "--help");
        Assert.Contains("Show detailed information", describeHelp);

        var (delHelp, _) = await ExecuteAsync("ups", "del", "--help");
        Assert.Contains("Delete a UPS unit", delHelp);
    }
}