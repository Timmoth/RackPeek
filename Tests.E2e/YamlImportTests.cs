using Microsoft.Playwright;
using Tests.E2e.Infra;
using Tests.E2e.PageObjectModels;
using Xunit.Abstractions;

namespace Tests.E2e;

/// <summary>
///     Coverage for /yaml/import and the /yaml config view.
///     Issue #308: connections in an imported document were parsed but silently
///     dropped — they never showed in the preview and never reached the config.
///     The preview assertions here are the regression guard; the /yaml
///     round-trip proves the connection actually persisted.
/// </summary>
public class YamlImportTests(
    PlaywrightFixture fixture,
    ITestOutputHelper output) : E2ETestBase(fixture, output) {
    private readonly PlaywrightFixture _fixture = fixture;
    private readonly ITestOutputHelper _output = output;

    private static string TwoSwitchesWithConnection(string switchA, string switchB) =>
        $"""
         version: 3
         resources:
           - kind: Switch
             name: {switchA}
             ports:
               - type: rj45
                 speed: 1
                 count: 4
           - kind: Switch
             name: {switchB}
             ports:
               - type: rj45
                 speed: 1
                 count: 4
         connections:
           - a:
               resource: {switchA}
               portGroup: 0
               portIndex: 0
             b:
               resource: {switchB}
               portGroup: 0
               portIndex: 0
         """;

    // =============================================================
    // Connections reach the preview (issue #308 regression)
    // =============================================================

    [Fact]
    public async Task Import_Preview_Lists_Connections_From_The_Document() {
        (IBrowserContext context, IPage page) = await CreatePageAsync();

        var switchA = $"e2e-ia-{Guid.NewGuid():N}"[..14];
        var switchB = $"e2e-ib-{Guid.NewGuid():N}"[..14];

        try {
            var import = new YamlImportPom(page);
            await import.GotoAsync(_fixture.BaseUrl);

            await import.PasteAsync(TwoSwitchesWithConnection(switchA, switchB));

            await import.AssertNoErrorAsync();

            // Before the #308 fix this section never rendered: the connection
            // was dropped between parsing and the diff.
            await import.AssertConnectionsSummaryVisibleAsync();
            await Assertions.Expect(import.ConnectionsAdded).ToHaveCountAsync(1);
            await import.AssertConnectionAddedContainsAsync(switchA);
            await import.AssertConnectionAddedContainsAsync(switchB);
        }
        catch (Exception) {
            await DumpAsync(page);
            throw;
        }
        finally {
            await context.CloseAsync();
        }
    }

    // =============================================================
    // Applying the import persists the connection
    // =============================================================

    [Fact]
    public async Task Applying_An_Import_Persists_Resources_And_Their_Connection() {
        (IBrowserContext context, IPage page) = await CreatePageAsync();

        var switchA = $"e2e-pa-{Guid.NewGuid():N}"[..14];
        var switchB = $"e2e-pb-{Guid.NewGuid():N}"[..14];

        try {
            var import = new YamlImportPom(page);
            await import.GotoAsync(_fixture.BaseUrl);

            await import.PasteAsync(TwoSwitchesWithConnection(switchA, switchB));
            await import.AssertNoErrorAsync();
            await import.ApplyAsync();

            // The saved config is the source of truth — read it back rather
            // than trusting the preview we just asserted on.
            await page.GotoAsync($"{_fixture.BaseUrl}/yaml");

            ILocator yaml = page.GetByTestId("yaml-file-content");
            await Assertions.Expect(yaml).ToBeVisibleAsync();

            await Assertions.Expect(yaml).ToContainTextAsync(switchA);
            await Assertions.Expect(yaml).ToContainTextAsync(switchB);
            await Assertions.Expect(yaml).ToContainTextAsync("connections:");
        }
        catch (Exception) {
            await DumpAsync(page);
            throw;
        }
        finally {
            await context.CloseAsync();
        }
    }

    // =============================================================
    // Malformed input is reported, not swallowed
    // =============================================================

    [Fact]
    public async Task Import_Surfaces_An_Error_For_Malformed_Yaml() {
        (IBrowserContext context, IPage page) = await CreatePageAsync();

        try {
            var import = new YamlImportPom(page);
            await import.GotoAsync(_fixture.BaseUrl);

            // Unclosed bracket — a parser error rather than a schema violation.
            await import.PasteAsync("""
                                    version: 3
                                    resources: [
                                      - kind: Switch
                                    """);

            await import.AssertErrorShownAsync();

            // A broken document must not be applyable.
            await Assertions.Expect(import.ApplyButton).ToBeDisabledAsync();
        }
        catch (Exception) {
            await DumpAsync(page);
            throw;
        }
        finally {
            await context.CloseAsync();
        }
    }

    // =============================================================
    // The prerender race (regression guard for the CI flake on #339)
    // =============================================================

    /// <summary>
    ///     The import page has the same prerender race the add form had (b8f6d23):
    ///     text filled before the circuit attaches never fires the oninput diff, so the
    ///     Apply button stays disabled forever. CI lost this race intermittently; the
    ///     injected latency loses it every time, proving GotoAsync's circuit wait fixes it.
    /// </summary>
    [Fact]
    public async Task Importing_Works_Before_The_Circuit_Has_Warmed_Up() {
        (IBrowserContext context, IPage page) = await CreatePageAsync();
        await BlazorLatency.AddAsync(page, TimeSpan.FromMilliseconds(300));

        var switchA = $"e2e-ra-{Guid.NewGuid():N}"[..14];
        var switchB = $"e2e-rb-{Guid.NewGuid():N}"[..14];

        try {
            var import = new YamlImportPom(page);
            await import.GotoAsync(_fixture.BaseUrl);

            await import.PasteAsync(TwoSwitchesWithConnection(switchA, switchB));

            // The regression: without GotoAsync waiting for the circuit, the fill above
            // is lost to the prerendered page, the diff never runs, and Apply stays
            // disabled. Asserting it becomes enabled is the whole point — persistence is
            // already covered by the non-latency test, and a post-apply navigation would
            // only reintroduce flakiness under the injected latency.
            await Assertions.Expect(import.ApplyButton).ToBeEnabledAsync();
        }
        catch (Exception) {
            await DumpAsync(page);
            throw;
        }
        finally {
            await context.CloseAsync();
        }
    }

    private async Task DumpAsync(IPage page) {
        _output.WriteLine("TEST FAILED — Capturing diagnostics");
        _output.WriteLine($"Current URL: {page.Url}");

        var html = await page.ContentAsync();
        _output.WriteLine("==== DOM SNAPSHOT START ====");
        _output.WriteLine(html);
        _output.WriteLine("==== DOM SNAPSHOT END ====");
    }
}
