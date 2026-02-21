using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.AccessPointTests;

[Collection("Yaml CLI tests")]
public class AccessPointErrorTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
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
    public async Task adding_duplicate_access_point_returns_error()
    {
        await ExecuteAsync("accesspoints", "add", "ap01");

        var (output, _) = await ExecuteAsync("accesspoints", "add", "ap01");

        Assert.Contains("already exists", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task get_missing_access_point_returns_error()
    {
        var (output, _) = await ExecuteAsync("accesspoints", "get", "ghost");

        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task set_missing_access_point_returns_error()
    {
        var (output, _) = await ExecuteAsync(
            "accesspoints", "set", "ghost",
            "--model", "X",
            "--speed", "1"
        );

        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task delete_missing_access_point_returns_error()
    {
        var (output, _) = await ExecuteAsync("accesspoints", "del", "ghost");

        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task invalid_speed_value_returns_error()
    {
        await ExecuteAsync("accesspoints", "add", "ap01");

        var (output, _) = await ExecuteAsync(
            "accesspoints", "set", "ap01",
            "--speed", "not-a-number"
        );

        Assert.Contains("error", output, StringComparison.OrdinalIgnoreCase);
    }
}