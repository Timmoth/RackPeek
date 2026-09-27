using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Tests.E2e.Infra;
using Tests.E2e.PageObjectModels;
using Xunit.Abstractions;

namespace Tests.E2e;

/// <summary>
///     Reproduces https://github.com/Timmoth/RackPeek/issues/333: when a stored
///     connection endpoint differs in case from the resource name (created by
///     `rpk connections add` or hand-edited/imported YAML), the Web UI treats
///     the port as unconnected — the card square stays grey with an "Available"
///     tooltip, and the details-page Ports panel shows "free" instead of a link
///     to the peer. Clicking the supposedly free port then crashes the Blazor
///     circuit. Both tests fail on staging until the endpoint comparisons (or
///     the stored casing) are fixed.
/// </summary>
public class ConnectionCasingTests(
    PlaywrightFixture fixture,
    ITestOutputHelper output) : E2ETestBase(fixture, output) {
    private readonly PlaywrightFixture _fixture = fixture;
    private readonly ITestOutputHelper _output = output;

    private static string CaseMismatchedConfig(string switchA, string switchB) =>
        $"""
         version: 3
         resources:
           - kind: Switch
             name: {switchA}
             ports:
               - type: rj45
                 speed: 1
                 count: 2
           - kind: Switch
             name: {switchB}
             ports:
               - type: rj45
                 speed: 1
                 count: 2
         connections:
           - a:
               resource: {switchA.ToLowerInvariant()}
               portGroup: 0
               portIndex: 0
             b:
               resource: {switchB.ToLowerInvariant()}
               portGroup: 0
               portIndex: 0
             label: probe-link
         """;

    [Fact]
    public async Task Card_Port_Square_Shows_The_Case_Mismatched_Connection() {
        (IBrowserContext context, IPage page) = await CreatePageAsync();

        var switchA = $"E2E-Csa-{Guid.NewGuid():N}"[..14];
        var switchB = $"E2E-Csb-{Guid.NewGuid():N}"[..14];

        try {
            await SeedAsync(page, switchA, switchB);

            await page.GotoAsync(
                $"{_fixture.BaseUrl}/resources/hardware/{Uri.EscapeDataString(switchA)}");

            var ports = new PortsPom(page);
            ILocator square = ports.Port("switch-ports", 0, 0);
            await Assertions.Expect(square).ToBeVisibleAsync();

            // A connected port's tooltip names the peer; "Available" means the
            // UI failed to match the stored connection to this port.
            await Assertions.Expect(square).Not.ToHaveAttributeAsync("title", "Available");
        }
        catch (Exception) {
            await DumpAsync(page);
            throw;
        }
        finally {
            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task Details_Ports_Panel_Links_To_The_Case_Mismatched_Peer() {
        (IBrowserContext context, IPage page) = await CreatePageAsync();

        var switchA = $"E2E-Cta-{Guid.NewGuid():N}"[..14];
        var switchB = $"E2E-Ctb-{Guid.NewGuid():N}"[..14];

        try {
            await SeedAsync(page, switchA, switchB);

            await page.GotoAsync(
                $"{_fixture.BaseUrl}/resources/hardware/{Uri.EscapeDataString(switchA)}");

            // A connected port renders as a NavLink to the peer's details page.
            // Today the row renders "1 - free" instead, and clicking it kills
            // the Blazor circuit.
            ILocator peerLink = page.Locator(
                $"a[href*='{Uri.EscapeDataString(switchB)}']");

            await Assertions.Expect(peerLink.First).ToBeVisibleAsync();

            await peerLink.First.ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(
                new Regex(Regex.Escape(Uri.EscapeDataString(switchB))));
        }
        catch (Exception) {
            await DumpAsync(page);
            throw;
        }
        finally {
            await context.CloseAsync();
        }
    }

    private async Task SeedAsync(IPage page, string switchA, string switchB) {
        var import = new YamlImportPom(page);
        await import.GotoAsync(_fixture.BaseUrl);
        await import.PasteAsync(CaseMismatchedConfig(switchA, switchB));
        await import.AssertNoErrorAsync();
        await import.ApplyAsync();
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
