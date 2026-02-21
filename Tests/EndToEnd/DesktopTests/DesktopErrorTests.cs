using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd;

[Collection("Yaml CLI tests")]
public class DesktopErrorTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
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
    public async Task adding_duplicate_desktop_returns_error()
    {
        await ExecuteAsync("desktops", "add", "workstation01");
        var (output, _) = await ExecuteAsync("desktops", "add", "workstation01");
        Assert.Contains("already exists", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task get_missing_desktop_returns_error()
    {
        var (output, _) = await ExecuteAsync("desktops", "get", "ghost");
        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task set_missing_desktop_returns_error()
    {
        var (output, _) = await ExecuteAsync("desktops", "set", "ghost", "--model", "X");
        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task delete_missing_desktop_returns_error()
    {
        var (output, _) = await ExecuteAsync("desktops", "del", "ghost");
        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task tree_missing_desktop_returns_error()
    {
        var (output, _) = await ExecuteAsync("desktops", "tree", "ghost");
        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    // CPU errors
    [Fact]
    public async Task cpu_add_missing_desktop_returns_error()
    {
        var (output, _) = await ExecuteAsync(
            "desktops", "cpu", "add", "ghost",
            "--model", "Xeon",
            "--cores", "8",
            "--threads", "16"
        );

        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task cpu_set_invalid_index_returns_error()
    {
        await ExecuteAsync("desktops", "add", "workstation01");

        var (output, _) = await ExecuteAsync(
            "desktops", "cpu", "set", "workstation01", "5",
            "--model", "Xeon"
        );

        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    // Drive errors
    [Fact]
    public async Task drive_add_missing_desktop_returns_error()
    {
        var (output, _) = await ExecuteAsync(
            "desktops", "drive", "add", "ghost",
            "--type", "ssd",
            "--size", "1000"
        );

        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    // GPU errors
    [Fact]
    public async Task gpu_add_missing_desktop_returns_error()
    {
        var (output, _) = await ExecuteAsync(
            "desktops", "gpu", "add", "ghost",
            "--model", "RTX 4090",
            "--vram", "24"
        );

        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    // NIC errors
    [Fact]
    public async Task nic_add_missing_desktop_returns_error()
    {
        var (output, _) = await ExecuteAsync(
            "desktops", "nic", "add", "ghost",
            "--type", "rj45",
            "--speed", "10",
            "--ports", "2"
        );

        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }
}
