using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.OtherTests;

[Collection("Yaml CLI tests")]
public class OtherWorkflowTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture> {
    private async Task<(string, string)> ExecuteAsync(params string[] args) {
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

    [Fact]
    public async Task other_cli_workflow_test() {
        // Add other hardware
        (var output, var yaml) = await ExecuteAsync("other", "add", "radio01");
        Assert.Equal("Other hardware 'radio01' added.\n", output);
        Assert.Contains("name: radio01", yaml);

        // Update other hardware
        (output, yaml) = await ExecuteAsync(
            "other", "set", "radio01",
            "--model", "Building-Bridge-XG",
            "--description", "Microwave radio bridge"
        );
        Assert.Equal("Other hardware 'radio01' updated.\n", output);

        Assert.Equal("""
                     version: 4
                     resources:
                     - kind: Other
                       model: Building-Bridge-XG
                       description: Microwave radio bridge
                       name: radio01
                     connections: []

                     """, yaml);

        // Add second other hardware
        (output, yaml) = await ExecuteAsync("other", "add", "bridge01");
        Assert.Equal("Other hardware 'bridge01' added.\n", output);

        (output, yaml) = await ExecuteAsync(
            "other", "set", "bridge01",
            "--model", "Interop-Bridge-2",
            "--description", "Radio interoperability bridge"
        );
        Assert.Equal("Other hardware 'bridge01' updated.\n", output);

        Assert.Equal("""
                     version: 4
                     resources:
                     - kind: Other
                       model: Building-Bridge-XG
                       description: Microwave radio bridge
                       name: radio01
                     - kind: Other
                       model: Interop-Bridge-2
                       description: Radio interoperability bridge
                       name: bridge01
                     connections: []

                     """, yaml);

        // Get other hardware
        (output, yaml) = await ExecuteAsync("other", "get", "radio01");
        Assert.Contains("radio01", output);
        Assert.Contains("Building-Bridge-XG", output);
        Assert.Contains("Microwave radio bridge", output);

        // List other hardware
        (output, yaml) = await ExecuteAsync("other", "list");
        Assert.Contains("radio01", output);
        Assert.Contains("bridge01", output);

        // Summary
        (output, yaml) = await ExecuteAsync("other", "summary");
        Assert.Contains("radio01", output);
        Assert.Contains("bridge01", output);

        // Delete other hardware
        (output, yaml) = await ExecuteAsync("other", "del", "bridge01");
        Assert.Equal("""
                     Other hardware 'bridge01' deleted.

                     """, output);

        // List again
        (output, yaml) = await ExecuteAsync("other", "list");
        Assert.Contains("radio01", output);
        Assert.DoesNotContain("bridge01", output);
    }
}
