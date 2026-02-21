using Tests.E2e.Infra;
using Tests.E2e.PageObjectModels;
using Xunit.Abstractions;

namespace Tests.E2e;

public class DesktopTests(
    PlaywrightFixture fixture,
    ITestOutputHelper output) : E2ETestBase(fixture, output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public async Task User_Can_Add_And_Delete_Desktop()
    {
        var (context, page) = await CreatePageAsync();
        var resourceName = $"e2e-ap-{Guid.NewGuid():N}"[..16];

        try
        {
            // Go home
            await page.GotoAsync(fixture.BaseUrl);

            _output.WriteLine($"URL after Goto: {page.Url}");

            var layout = new MainLayoutPom(page);
            await layout.AssertLoadedAsync();
            await layout.GotoHardwareAsync();

            var hardwarePage = new HardwareTreePom(page);
            await hardwarePage.AssertLoadedAsync();
            await hardwarePage.GotoDesktopsListAsync();

            var listPage = new DesktopsListPom(page);
            await listPage.AssertLoadedAsync();
            await listPage.AddDesktopAsync(resourceName);
            await listPage.AssertDesktopExists(resourceName);
            await listPage.DeleteDesktopAsync(resourceName);
            await listPage.AssertDesktopDoesNotExist(resourceName);

            await context.CloseAsync();
        }
        catch (Exception ex)
        {
            _output.WriteLine("TEST FAILED — Capturing diagnostics");

            _output.WriteLine($"Current URL: {page.Url}");

            var html = await page.ContentAsync();
            _output.WriteLine("==== DOM SNAPSHOT START ====");
            _output.WriteLine(html);
            _output.WriteLine("==== DOM SNAPSHOT END ====");

            throw;
        }
        finally
        {
            await context.CloseAsync();
        }
    }
}