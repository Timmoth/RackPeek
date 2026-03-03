using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.ServerTests;

[Collection("Yaml CLI tests")]
public class ServerErrorTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
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
    public async Task adding_duplicate_server_returns_error() {
        await ExecuteAsync("servers", "add", "srv01");
        (var output, var _) = await ExecuteAsync("servers", "add", "srv01");
        Assert.Contains("already exists", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task get_missing_server_returns_error() {
        (var output, var _) = await ExecuteAsync("servers", "get", "ghost");
        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task set_missing_server_returns_error() {
        (var output, var _) = await ExecuteAsync("servers", "set", "ghost", "--ram", "64");
        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task delete_missing_server_returns_error() {
        (var output, var _) = await ExecuteAsync("servers", "del", "ghost");
        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    // CPU errors
    [Fact]
    public async Task cpu_add_missing_server_returns_error() {
        (var output, var _) = await ExecuteAsync(
            "servers", "cpu", "add", "ghost",
            "--model", "Xeon",
            "--cores", "8",
            "--threads", "16"
        );

        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task cpu_set_invalid_index_returns_error() {
        await ExecuteAsync("servers", "add", "srv01");

        (var output, var _) = await ExecuteAsync(
            "servers", "cpu", "set", "srv01",
            "--index", "5",
            "--model", "Xeon"
        );

        Assert.Contains("Not found", output, StringComparison.OrdinalIgnoreCase);
    }

    // Drive errors
    [Fact]
    public async Task drive_set_invalid_index_returns_error() {
        await ExecuteAsync("servers", "add", "srv01");

        (var output, var _) = await ExecuteAsync(
            "servers", "drive", "set", "srv01",
            "--index", "3",
            "--type", "ssd"
        );

        Assert.Contains("Not found", output, StringComparison.OrdinalIgnoreCase);
    }

    // GPU errors
    [Fact]
    public async Task gpu_set_invalid_index_returns_error() {
        await ExecuteAsync("servers", "add", "srv01");

        (var output, var _) = await ExecuteAsync(
            "servers", "gpu", "set", "srv01",
            "--index", "2",
            "--model", "A2000"
        );

        Assert.Contains("Not found", output, StringComparison.OrdinalIgnoreCase);
    }

    // NIC errors
    [Fact]
    public async Task nic_set_invalid_index_returns_error() {
        await ExecuteAsync("servers", "add", "srv01");

        (var output, var _) = await ExecuteAsync(
            "servers", "nic", "set", "srv01",
            "--index", "4",
            "--type", "ethernet"
        );

        Assert.Contains("Validation error", output, StringComparison.OrdinalIgnoreCase);
    }
}
