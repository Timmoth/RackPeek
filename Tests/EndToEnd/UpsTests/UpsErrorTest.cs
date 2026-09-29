using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.UpsTests;

[Collection("Process-wide static state")]
public class UpsErrorTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture> {
    private async Task<(string, string)> ExecuteAsync(params string[] args) {
        var output = await YamlCliTestHost.RunAsync(
            args,
            fs.Root,
            outputHelper,
            "config.yaml");

        var yaml = await File.ReadAllTextAsync(Path.Combine(fs.Root, "config.yaml"));
        return (output, yaml);
    }

    [Fact]
    public async Task adding_duplicate_ups_returns_error() {
        await ExecuteAsync("ups", "add", "ups01");

        (var output, var _) = await ExecuteAsync("ups", "add", "ups01");

        Assert.Contains("already exists", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task get_missing_ups_returns_error() {
        (var output, var _) = await ExecuteAsync("ups", "get", "ghost");

        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task set_missing_ups_returns_error() {
        (var output, var _) = await ExecuteAsync(
            "ups", "set", "ghost",
            "--model", "X",
            "--va", "1000"
        );

        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task delete_missing_ups_returns_error() {
        (var output, var _) = await ExecuteAsync("ups", "del", "ghost");

        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task invalid_va_value_returns_error() {
        await ExecuteAsync("ups", "add", "ups01");

        (var output, var _) = await ExecuteAsync(
            "ups", "set", "ups01",
            "--va", "not-a-number"
        );

        Assert.Contains("error", output, StringComparison.OrdinalIgnoreCase);
    }


    // Port errors
    [Fact]
    public async Task port_add_missing_ups_returns_error() {
        (var output, var _) = await ExecuteAsync(
            "ups", "port", "add", "ghost",
            "--type", "usb",
            "--speed", "0.48",
            "--count", "1"
        );

        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task port_add_invalid_type_returns_error() {
        await ExecuteAsync("ups", "add", "ups01");

        (var output, var _) = await ExecuteAsync(
            "ups", "port", "add", "ups01",
            "--type", "not-a-port-type",
            "--speed", "1",
            "--count", "1"
        );

        Assert.Contains("not valid", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task port_set_invalid_index_returns_error() {
        await ExecuteAsync("ups", "add", "ups01");

        (var output, var _) = await ExecuteAsync(
            "ups", "port", "set", "ups01",
            "--index", "5",
            "--type", "usb"
        );

        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task port_del_invalid_index_returns_error() {
        await ExecuteAsync("ups", "add", "ups01");

        (var output, var _) = await ExecuteAsync(
            "ups", "port", "del", "ups01",
            "--index", "3"
        );

        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }
}
