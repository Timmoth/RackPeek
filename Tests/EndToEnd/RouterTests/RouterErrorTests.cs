using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.RouterTests;

[Collection("Yaml CLI tests")]
public class RouterErrorTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture> {
    private async Task<(string, string)> ExecuteAsync(params string[] args) {
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
    public async Task adding_duplicate_router_returns_error() {
        await ExecuteAsync("routers", "add", "rt01");
        (var output, var _) = await ExecuteAsync("routers", "add", "rt01");
        Assert.Contains("already exists", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task get_missing_router_returns_error() {
        (var output, var _) = await ExecuteAsync("routers", "get", "ghost");
        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task set_missing_router_returns_error() {
        (var output, var _) = await ExecuteAsync("routers", "set", "ghost", "--Model", "X");
        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task delete_missing_router_returns_error() {
        (var output, var _) = await ExecuteAsync("routers", "del", "ghost");
        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    // Port errors
    [Fact]
    public async Task port_add_missing_router_returns_error() {
        (var output, var _) = await ExecuteAsync(
            "routers", "port", "add", "ghost",
            "--type", "rj45",
            "--speed", "1",
            "--count", "8"
        );

        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task port_set_invalid_index_returns_error() {
        await ExecuteAsync("routers", "add", "rt01");

        (var output, var _) = await ExecuteAsync(
            "routers", "port", "set", "rt01",
            "--index", "5",
            "--type", "rj45"
        );

        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task port_del_invalid_index_returns_error() {
        await ExecuteAsync("routers", "add", "rt01");

        (var output, var _) = await ExecuteAsync(
            "routers", "port", "del", "rt01",
            "--index", "3"
        );

        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }
}
