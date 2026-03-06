using Microsoft.Playwright;
using Tests.E2e.Infra;
using Tests.E2e.PageObjectModels;
using Xunit.Abstractions;

namespace Tests.E2e;

public class FirewallTests(
    PlaywrightFixture fixture,
    ITestOutputHelper output) : E2ETestBase(fixture, output) {
    private readonly PlaywrightFixture _fixture = fixture;
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public async Task User_Can_Add_And_Delete_Firewall() {
        (IBrowserContext context, IPage page) = await CreatePageAsync();
        var resourceName = $"e2e-ap-{Guid.NewGuid():N}"[..16];

        try {
            // Go home
            await page.GotoAsync(_fixture.BaseUrl);

            _output.WriteLine($"URL after Goto: {page.Url}");

            var layout = new MainLayoutPom(page);
            await layout.AssertLoadedAsync();
            await layout.GotoHardwareAsync();

            var hardwarePage = new HardwareTreePom(page);
            await hardwarePage.AssertLoadedAsync();
            await hardwarePage.GotoFirewallsListAsync();

            var listPage = new FirewallsListPom(page);
            await listPage.AssertLoadedAsync();
            await listPage.AddFirewallAsync(resourceName);
            await listPage.AssertFirewallExists(resourceName);
            await listPage.DeleteFirewallAsync(resourceName);
            await listPage.AssertFirewallDoesNotExist(resourceName);

            await context.CloseAsync();
        }
        catch (Exception) {
            _output.WriteLine("TEST FAILED — Capturing diagnostics");

            _output.WriteLine($"Current URL: {page.Url}");

            var html = await page.ContentAsync();
            _output.WriteLine("==== DOM SNAPSHOT START ====");
            _output.WriteLine(html);
            _output.WriteLine("==== DOM SNAPSHOT END ====");

            throw;
        }
        finally {
            await context.CloseAsync();
        }
    }
}
