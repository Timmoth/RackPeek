using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd;

// Reproduces the load-side half of
// https://github.com/Timmoth/RackPeek/issues/337: saves rewrite config.yaml in
// place (File.WriteAllTextAsync truncates before writing, and never flushes),
// so an interrupted save leaves a truncated file behind — and a truncated file
// is then accepted without complaint on the next load.
//
// The writer-side half (make PhysicalTextFileStore write atomically and
// durably: temp file + flush + rename) is not observable from a black-box
// test; these tests pin the user-facing contract that a damaged file must not
// be served silently or crash with a raw stack trace.
[Collection("Yaml CLI tests")]
public class CorruptConfigTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture> {
    // Exactly what the serializer writes for three bare servers.
    private const string _fullConfig = """
                                       version: 4
                                       resources:
                                       - kind: Server
                                         name: srv-a
                                       - kind: Server
                                         name: srv-b
                                       - kind: Server
                                         name: srv-c
                                       connections: []

                                       """;

    private async Task<string> ExecuteAsync(params string[] args) {
        outputHelper.WriteLine($"rpk {string.Join(" ", args)}");

        var output = await YamlCliTestHost.RunAsync(
            args,
            fs.Root,
            outputHelper,
            "config.yaml");

        outputHelper.WriteLine(output);
        return output;
    }

    [Fact]
    public async Task a_config_truncated_at_a_resource_boundary_is_not_served_silently() {
        // Simulate an interrupted in-place save: the file ends mid-way through
        // the resources list. This still parses — as a plausible, smaller
        // inventory missing srv-c and the connections section.
        var truncated = _fullConfig[.._fullConfig.IndexOf("- kind: Server\n  name: srv-c", StringComparison.Ordinal)];
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), truncated);

        var output = await ExecuteAsync("summary");

        // Serving a structurally incomplete file (a resources document with no
        // connections section — something RackPeek's own serializer never
        // writes) with no diagnostic at all is how a truncation becomes silent
        // data loss: the next save persists the smaller inventory as if it
        // were intentional.
        var hasDiagnostic =
            output.Contains("error", StringComparison.OrdinalIgnoreCase)
            || output.Contains("warn", StringComparison.OrdinalIgnoreCase)
            || output.Contains("corrupt", StringComparison.OrdinalIgnoreCase)
            || output.Contains("incomplete", StringComparison.OrdinalIgnoreCase)
            || output.Contains("truncated", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasDiagnostic,
            $"A truncated config was served with no diagnostic. Output:\n{output}");
    }

    [Fact]
    public async Task a_config_cut_mid_token_fails_with_a_friendly_error() {
        // Simulate a save that died mid-write inside a YAML token.
        var truncated = _fullConfig[.._fullConfig.IndexOf("nd: Server\n  name: srv-c", StringComparison.Ordinal)];
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), truncated);

        var output = await ExecuteAsync("summary");

        // Observed on staging: the unparseable file is swallowed entirely and
        // `rpk summary` reports an EMPTY inventory (Hardware (0)) with exit 0 —
        // no error, no mention of the file. That is the worst outcome for
        // #337: a later write would persist the empty inventory over the
        // damaged-but-recoverable file. The user should instead get an
        // actionable message naming the config file, and no stack dump.
        Assert.DoesNotContain("at RackPeek.", output);
        Assert.DoesNotContain("YamlDotNet.Core", output);
        Assert.Contains("config.yaml", output);
    }
}
