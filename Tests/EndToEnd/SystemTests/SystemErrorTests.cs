using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.SystemTests;

[Collection("Yaml CLI tests")]
public class SystemErrorTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
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
    public async Task adding_duplicate_system_returns_error()
    {
        await ExecuteAsync("systems", "add", "sys01");
        var (output, _) = await ExecuteAsync("systems", "add", "sys01");
        Assert.Contains("already exists", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task get_missing_system_returns_error()
    {
        var (output, _) = await ExecuteAsync("systems", "get", "ghost");
        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task set_missing_system_returns_error()
    {
        var (output, _) = await ExecuteAsync("systems", "set", "ghost", "--type", "vm");
        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task delete_missing_system_returns_error()
    {
        var (output, _) = await ExecuteAsync("systems", "del", "ghost");
        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task tree_missing_system_returns_error()
    {
        var (output, _) = await ExecuteAsync("systems", "tree", "ghost");
        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }
}