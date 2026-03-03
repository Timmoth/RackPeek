using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.LaptopTests;

[Collection("Yaml CLI tests")]
public class LaptopErrorTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
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
    public async Task adding_duplicate_laptop_returns_error() {
        await ExecuteAsync("laptops", "add", "lap01");
        (var output, var _) = await ExecuteAsync("laptops", "add", "lap01");
        Assert.Contains("already exists", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task get_missing_laptop_returns_error() {
        (var output, var _) = await ExecuteAsync("laptops", "get", "ghost");
        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task delete_missing_laptop_returns_error() {
        (var output, var _) = await ExecuteAsync("laptops", "del", "ghost");
        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    // CPU errors
    [Fact]
    public async Task cpu_add_missing_laptop_returns_error() {
        (var output, var _) = await ExecuteAsync(
            "laptops", "cpu", "add", "ghost",
            "--model", "Intel i7",
            "--cores", "8",
            "--threads", "16"
        );

        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task cpu_set_invalid_index_returns_error() {
        await ExecuteAsync("laptops", "add", "lap01");

        (var output, var _) = await ExecuteAsync(
            "laptops", "cpu", "set", "lap01", "5",
            "--model", "Intel i7"
        );

        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    // Drive errors
    [Fact]
    public async Task drive_set_invalid_index_returns_error() {
        await ExecuteAsync("laptops", "add", "lap01");

        (var output, var _) = await ExecuteAsync(
            "laptops", "drives", "set", "lap01", "3",
            "--type", "ssd"
        );

        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    // GPU errors
    [Fact]
    public async Task gpu_set_invalid_index_returns_error() {
        await ExecuteAsync("laptops", "add", "lap01");

        (var output, var _) = await ExecuteAsync(
            "laptops", "gpu", "set", "lap01", "2",
            "--model", "Intel Iris Xe"
        );

        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }
}
