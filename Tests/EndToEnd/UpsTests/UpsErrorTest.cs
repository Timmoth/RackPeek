using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.UpsTests;

[Collection("Yaml CLI tests")]
public class UpsErrorTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
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
    public async Task adding_duplicate_ups_returns_error()
    {
        await ExecuteAsync("ups", "add", "ups01");

        var (output, _) = await ExecuteAsync("ups", "add", "ups01");

        Assert.Contains("already exists", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task get_missing_ups_returns_error()
    {
        var (output, _) = await ExecuteAsync("ups", "get", "ghost");

        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task set_missing_ups_returns_error()
    {
        var (output, _) = await ExecuteAsync(
            "ups", "set", "ghost",
            "--model", "X",
            "--va", "1000"
        );

        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task delete_missing_ups_returns_error()
    {
        var (output, _) = await ExecuteAsync("ups", "del", "ghost");

        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task invalid_va_value_returns_error()
    {
        await ExecuteAsync("ups", "add", "ups01");

        var (output, _) = await ExecuteAsync(
            "ups", "set", "ups01",
            "--va", "not-a-number"
        );

        Assert.Contains("error", output, StringComparison.OrdinalIgnoreCase);
    }
}