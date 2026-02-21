using Tests.E2e.Infra;
using Tests.E2e.PageObjectModels;
using Xunit.Abstractions;

namespace Tests.E2e;

public class ServerTests(
    PlaywrightFixture fixture,
    ITestOutputHelper output) : E2ETestBase(fixture, output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public async Task User_Can_Add_And_Delete_Server()
    {
        var (context, page) = await CreatePageAsync();
        var serverName = $"e2e-server-{Guid.NewGuid():N}"[..16];

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
            await hardwarePage.GotoServersListAsync();

            var serverListPage = new ServersListPom(page);
            await serverListPage.AssertLoadedAsync();
            await serverListPage.AddServerAsync(serverName);
            await serverListPage.AssertServerExists(serverName);
            await serverListPage.DeleteServerAsync(serverName);
            await serverListPage.AssertServerDoesNotExist(serverName);

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