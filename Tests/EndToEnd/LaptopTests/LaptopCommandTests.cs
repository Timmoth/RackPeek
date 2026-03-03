using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.LaptopTests;

[Collection("Yaml CLI tests")]
public class LaptopCommandTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
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
    public async Task describe_outputs_expected_information() {
        await ExecuteAsync("laptops", "add", "lap01");
        await ExecuteAsync("laptops", "set", "lap01", "--model", "ThinkPad X1 Carbon");

        (var output, var _) = await ExecuteAsync("laptops", "describe", "lap01");

        Assert.Contains("lap01", output);
    }

    [Fact]
    public async Task help_commands_do_not_throw() {
        Assert.Contains("Manage Laptop computers", (await ExecuteAsync("laptops", "--help")).Item1);
        Assert.Contains("Add a new Laptop", (await ExecuteAsync("laptops", "add", "--help")).Item1);
        Assert.Contains("List all Laptops", (await ExecuteAsync("laptops", "list", "--help")).Item1);
        Assert.Contains("Retrieve a Laptop", (await ExecuteAsync("laptops", "get", "--help")).Item1);
        Assert.Contains("Show detailed information", (await ExecuteAsync("laptops", "describe", "--help")).Item1);
        Assert.Contains("Delete a Laptop", (await ExecuteAsync("laptops", "del", "--help")).Item1);
        Assert.Contains("Show a summarized hardware report",
            (await ExecuteAsync("laptops", "summary", "--help")).Item1);
        Assert.Contains("Display the dependency tree", (await ExecuteAsync("laptops", "tree", "--help")).Item1);

        // CPU help
        Assert.Contains("Manage CPUs", (await ExecuteAsync("laptops", "cpu", "--help")).Item1);
        Assert.Contains("Add a CPU", (await ExecuteAsync("laptops", "cpu", "add", "--help")).Item1);

        // Drives help
        Assert.Contains("Manage storage drives", (await ExecuteAsync("laptops", "drives", "--help")).Item1);

        // GPU help
        Assert.Contains("Manage GPUs", (await ExecuteAsync("laptops", "gpu", "--help")).Item1);
    }
}
