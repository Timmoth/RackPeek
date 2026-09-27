using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd;

// Reproduces https://github.com/Timmoth/RackPeek/issues/328 (case D):
// a case-only rename such as `rpk servers rename Srv01 srv01` is rejected with
// "Conflict: Server resource 'srv01' already exists", because the existence
// check matches the resource against itself. Agreed behaviour on the issue is
// that a case-only rename should proceed as a re-casing, while renaming onto a
// case variant of a *different* resource stays a conflict.
[Collection("Yaml CLI tests")]
public class RenameCasingTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
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

    [Fact]
    public async Task case_only_rename_recases_the_resource() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        await ExecuteAsync("servers", "add", "Srv01");

        (var output, var yaml) = await ExecuteAsync("servers", "rename", "Srv01", "srv01");

        Assert.Equal("Server 'Srv01' renamed to 'srv01'.\n", output);
        Assert.Contains("name: srv01", yaml);
        Assert.DoesNotContain("name: Srv01", yaml);
    }

    [Fact]
    public async Task case_only_rename_updates_dependants_and_connections() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        await ExecuteAsync("servers", "add", "Srv01");
        await ExecuteAsync("systems", "add", "vm01", "--runs-on", "Srv01");

        (var output, var yaml) = await ExecuteAsync("servers", "rename", "Srv01", "srv01");

        Assert.Equal("Server 'Srv01' renamed to 'srv01'.\n", output);
        Assert.Contains("- srv01", yaml);
        Assert.DoesNotContain("- Srv01", yaml);
    }

    [Fact]
    public async Task rename_onto_a_case_variant_of_another_resource_is_still_a_conflict() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        await ExecuteAsync("servers", "add", "Srv01");
        await ExecuteAsync("servers", "add", "srv02");

        (var output, var yaml) = await ExecuteAsync("servers", "rename", "srv02", "SRV01");

        Assert.Contains("already exists", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("name: Srv01", yaml);
        Assert.Contains("name: srv02", yaml);
    }
}
