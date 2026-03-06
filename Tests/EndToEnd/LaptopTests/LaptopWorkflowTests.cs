using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.LaptopTests;

[Collection("Yaml CLI tests")]
public class LaptopWorkflowTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture>
{
    private async Task<(string, string)> ExecuteAsync(params string[] args)
    {
        outputHelper.WriteLine($"rpk {string.Join(" ", args)}");

        var output = await YamlCliTestHost.RunAsync(
            args,
            fs.Root,
            outputHelper,
            "config.yaml"
        );

        outputHelper.WriteLine(output);

        var yaml = await File.ReadAllTextAsync(Path.Combine(fs.Root, "config.yaml"));
        return (output, yaml);
    }

    [Fact]
    public async Task laptops_cli_workflow_test()
    {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Add laptop
        var (output, yaml) = await ExecuteAsync("laptops", "add", "lap01");
        Assert.Equal("Laptop 'lap01' added.\n", output);
        Assert.Contains("name: lap01", yaml);

        // Set model
        (output, yaml) = await ExecuteAsync(
            "laptops", "set", "lap01",
            "--model", "ThinkPad X1 Carbon"
        );
        Assert.Equal("Laptop 'lap01' updated.\n", output);
        Assert.Contains("model: ThinkPad X1 Carbon", yaml);

        // Add CPU
        (output, yaml) = await ExecuteAsync(
            "laptops", "cpu", "add", "lap01",
            "--model", "Intel i7-1260P",
            "--cores", "12",
            "--threads", "16"
        );
        Assert.Equal("CPU added to Laptop 'lap01'.\n", output);

        // Add Drive
        (output, yaml) = await ExecuteAsync(
            "laptops", "drives", "add", "lap01",
            "--type", "ssd",
            "--size", "512"
        );
        Assert.Equal("Drive added to Laptop 'lap01'.\n", output);

        // Add GPU
        (output, yaml) = await ExecuteAsync(
            "laptops", "gpu", "add", "lap01",
            "--model", "Intel Iris Xe",
            "--vram", "1"
        );
        Assert.Equal("GPU added to Laptop 'lap01'.\n", output);

        // Get laptop (rich one-line output)
        (output, yaml) = await ExecuteAsync("laptops", "get", "lap01");
        Assert.Equal(
            "lap01\n",
            output
        );

        // List laptops 
        (output, yaml) = await ExecuteAsync("laptops", "list");
        Assert.Contains("lap01", output);

        // Summary 
        (output, yaml) = await ExecuteAsync("laptops", "summary");
        Assert.Contains("lap01", output);

        // Describe 
        (output, yaml) = await ExecuteAsync("laptops", "describe", "lap01");
        Assert.Contains("lap01", output);

        // Tree 
        (output, yaml) = await ExecuteAsync("laptops", "tree", "lap01");
        Assert.Contains("lap01", output);

        // Delete laptop
        (output, yaml) = await ExecuteAsync("laptops", "del", "lap01");
        Assert.Equal("""
                     Laptop 'lap01' deleted.

                     """, output);
    }
}
