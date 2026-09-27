using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.ConnectionTests;

// Reproduces https://github.com/Timmoth/RackPeek/issues/333 (CLI side):
// `rpk connections add` stores the endpoint names as typed. ValidatePortReference
// looks the resource up case-insensitively but never keeps its canonical Name,
// so `connections add srv01 ...` against a resource named `Srv01` persists
// `resource: srv01`. The Web UI then compares endpoints case-sensitively and
// treats the port as unconnected.
[Collection("Yaml CLI tests")]
public class ConnectionCasingTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
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
    public async Task connection_endpoints_are_stored_with_the_canonical_resource_casing() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        await ExecuteAsync("switches", "add", "Core-SW-A");
        await ExecuteAsync("switches", "add", "Core-SW-B");

        await ExecuteAsync(
            "switches", "port", "add", "Core-SW-A",
            "--type", "rj45", "--speed", "1", "--count", "2");
        await ExecuteAsync(
            "switches", "port", "add", "Core-SW-B",
            "--type", "rj45", "--speed", "1", "--count", "2");

        // Endpoints typed in a different case from the stored names.
        (var output, var yaml) = await ExecuteAsync(
            "connections", "add",
            "core-sw-a", "0", "0",
            "core-sw-b", "0", "0",
            "--label", "probe-link");

        Assert.Contains("Connection created", output);

        // The stored endpoint must carry the canonical resource name, or every
        // case-sensitive consumer (PortLayout, PortGroupVisualizer, the
        // connection modal) treats the port as free.
        Assert.Contains("resource: Core-SW-A", yaml);
        Assert.Contains("resource: Core-SW-B", yaml);
        Assert.DoesNotContain("resource: core-sw-a", yaml);
        Assert.DoesNotContain("resource: core-sw-b", yaml);
    }
}
