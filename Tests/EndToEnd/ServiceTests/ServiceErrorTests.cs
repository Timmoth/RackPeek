using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.ServiceTests;

[Collection("Yaml CLI tests")]
public class ServiceErrorTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
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
    public async Task adding_duplicate_service_returns_error()
    {
        await ExecuteAsync("services", "add", "svc01");
        var (output, _) = await ExecuteAsync("services", "add", "svc01");
        Assert.Contains("already exists", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task get_missing_service_returns_error()
    {
        var (output, _) = await ExecuteAsync("services", "get", "ghost");
        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task set_missing_service_returns_error()
    {
        var (output, _) = await ExecuteAsync("services", "set", "ghost", "--ip", "1.2.3.4");
        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task delete_missing_service_returns_error()
    {
        var (output, _) = await ExecuteAsync("services", "del", "ghost");
        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task set_runs_on_missing_system_returns_error()
    {
        await ExecuteAsync("services", "add", "svc01");

        var (output, _) = await ExecuteAsync(
            "services", "set", "svc01",
            "--runs-on", "ghost"
        );

        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }
}
