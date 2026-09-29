using Microsoft.Playwright;
using Tests.E2e.Infra;
using Tests.E2e.PageObjectModels;
using Xunit.Abstractions;

namespace Tests.E2e;

/// <summary>
///     The breadcrumb used to label every System with its storage kind, so a guest on a
///     hypervisor on a server read "host (server) / host-pve (system) / guest (system)" —
///     the chain existed to show the nesting and then hid what each layer actually was.
///     A System now reports its own type instead.
/// </summary>
public class BreadcrumbTypeTests(
    PlaywrightFixture fixture,
    ITestOutputHelper output) : E2ETestBase(fixture, output) {
    private readonly PlaywrightFixture _fixture = fixture;
    private readonly ITestOutputHelper _output = output;

    private static string HypervisorStack(string server, string hypervisor, string guest) =>
        $"""
         version: 4
         resources:
           - kind: Server
             name: {server}
           - kind: System
             name: {hypervisor}
             type: hypervisor
             runsOn:
               - {server}
           - kind: System
             name: {guest}
             type: vm
             runsOn:
               - {hypervisor}
         connections: []
         """;

    [Fact]
    public async Task A_breadcrumb_names_each_layers_type_not_its_storage_kind() {
        (IBrowserContext context, IPage page) = await CreatePageAsync();

        var server = $"e2e-bcs-{Guid.NewGuid():N}"[..14];
        var hypervisor = $"e2e-bch-{Guid.NewGuid():N}"[..14];
        var guest = $"e2e-bcg-{Guid.NewGuid():N}"[..14];

        try {
            var import = new YamlImportPom(page);
            await import.GotoAsync(_fixture.BaseUrl);
            await import.PasteAsync(HypervisorStack(server, hypervisor, guest));
            await import.AssertNoErrorAsync();
            await import.ApplyAsync();

            await page.GotoAsync(
                $"{_fixture.BaseUrl}/resources/systems/{Uri.EscapeDataString(guest)}");

            await Assertions.Expect(page.GetByTestId("circuit-probe"))
                .ToHaveAttributeAsync("data-circuit-ready", "true");

            var body = await page.InnerTextAsync("body");

            Assert.Contains("(Server)", body);
            Assert.Contains("(Hypervisor)", body);
            Assert.Contains("(VM)", body);

            // The kind must no longer stand in for the two systems' types.
            Assert.DoesNotContain("(system)", body, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception) {
            _output.WriteLine($"TEST FAILED — URL: {page.Url}");
            _output.WriteLine(await page.ContentAsync());
            throw;
        }
        finally {
            await context.CloseAsync();
        }
    }
}
