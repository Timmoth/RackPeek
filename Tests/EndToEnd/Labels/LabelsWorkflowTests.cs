using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.Labels;

[Collection("Yaml CLI tests")]
public class LabelsWorkflowTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture> {
    private async Task<(string output, string yaml)> ExecuteAsync(params string[] args) {
        outputHelper.WriteLine($"rpk {string.Join(" ", args)}");

        var output = await YamlCliTestHost.RunAsync(
            args,
            fs.Root,
            outputHelper,
            "config.yaml");

        outputHelper.WriteLine(output);

        var yaml = await File.ReadAllTextAsync(Path.Combine(fs.Root, "config.yaml"));
        return (output, yaml);
    }


    [Theory]
    [InlineData("servers")]
    [InlineData("accesspoints")]
    [InlineData("desktops")]
    [InlineData("laptops")]
    [InlineData("firewalls")]
    [InlineData("routers")]
    [InlineData("services")]
    [InlineData("systems")]
    [InlineData("ups")]
    public async Task labels_cli_workflow_test(string resourceCommand) {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Create server
        (var output, var yaml) = await ExecuteAsync(resourceCommand, "add", "web-01");
        Assert.Contains("web-01", yaml);

        // Add label
        (output, yaml) = await ExecuteAsync(resourceCommand, "label", "add", "web-01", "--key", "env", "--value",
            "production");
        Assert.Contains("Label 'env' added", output);
        Assert.Contains("env:", yaml);
        Assert.Contains("production", yaml);

        // Describe should show label
        (output, _) = await ExecuteAsync(resourceCommand, "describe", "web-01");
        Assert.Contains("env", output);
        Assert.Contains("production", output);

        // Remove label
        (output, yaml) = await ExecuteAsync(resourceCommand, "label", "remove", "web-01", "--key", "env");
        Assert.Contains("Label 'env' removed", output);
        Assert.DoesNotContain("env:", yaml);
    }
}
