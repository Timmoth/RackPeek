using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd;

[Collection("Yaml CLI tests")]
public class DesktopCommandTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
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
    public async Task describe_returns_detailed_information()
    {
        // given 
        await ExecuteAsync("desktops", "add", "workstation01");
        await ExecuteAsync("desktops", "set", "workstation01", "--model", "Dell Precision 7960");

        // then
        var (output, _) = await ExecuteAsync("desktops", "describe", "workstation01");

        // when
        Assert.Contains("Name:", output);
        Assert.Contains("workstation01", output);
        Assert.Contains("Model:", output);
        Assert.Contains("Dell Precision 7960", output);
    }

    [Fact]
    public async Task help_outputs_do_not_throw()
    {
        Assert.Contains("Manage desktop computers", (await ExecuteAsync("desktops", "--help")).Item1);
        Assert.Contains("Add a new desktop", (await ExecuteAsync("desktops", "add", "--help")).Item1);
        Assert.Contains("Retrieve a desktop", (await ExecuteAsync("desktops", "get", "--help")).Item1);
        Assert.Contains("Show detailed information", (await ExecuteAsync("desktops", "describe", "--help")).Item1);
        Assert.Contains("Update properties", (await ExecuteAsync("desktops", "set", "--help")).Item1);
        Assert.Contains("Delete a desktop", (await ExecuteAsync("desktops", "del", "--help")).Item1);
        Assert.Contains("summarized hardware report", (await ExecuteAsync("desktops", "summary", "--help")).Item1);
        Assert.Contains("dependency tree", (await ExecuteAsync("desktops", "tree", "--help")).Item1);

        // Component help
        Assert.Contains("Manage CPUs", (await ExecuteAsync("desktops", "cpu", "--help")).Item1);
        Assert.Contains("Add a CPU", (await ExecuteAsync("desktops", "cpu", "add", "--help")).Item1);
        Assert.Contains("Update a desktop CPU", (await ExecuteAsync("desktops", "cpu", "set", "--help")).Item1);

        Assert.Contains("Manage storage drives", (await ExecuteAsync("desktops", "drive", "--help")).Item1);
        Assert.Contains("Add a drive", (await ExecuteAsync("desktops", "drive", "add", "--help")).Item1);

        Assert.Contains("Manage GPUs", (await ExecuteAsync("desktops", "gpu", "--help")).Item1);
        Assert.Contains("Add a GPU", (await ExecuteAsync("desktops", "gpu", "add", "--help")).Item1);

        Assert.Contains("Manage network interface cards", (await ExecuteAsync("desktops", "nic", "--help")).Item1);
        Assert.Contains("Add a NIC", (await ExecuteAsync("desktops", "nic", "add", "--help")).Item1);
    }
}
