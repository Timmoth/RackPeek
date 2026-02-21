using Tests.E2e.Infra;
using Tests.E2e.PageObjectModels;
using Xunit.Abstractions;

namespace Tests.E2e;

public class SystemTests(
    PlaywrightFixture fixture,
    ITestOutputHelper output) : E2ETestBase(fixture, output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public async Task User_Can_Add_And_Delete_System()
    {
        var (context, page) = await CreatePageAsync();

        try
        {
            // Go home
            await page.GotoAsync(fixture.BaseUrl);

            _output.WriteLine($"URL after Goto: {page.Url}");

            var layout = new MainLayoutPom(page);
            await layout.AssertLoadedAsync();
            await layout.GotoSystemsAsync();

            var systems = new SystemsListPom(page);

            var systemName = $"e2e-system-{Guid.NewGuid():N}"[..12];

            await systems.AddSystemAsync(systemName);

            await systems.AssertSystemExists(systemName);

            await systems.DeleteSystemAsync(systemName);

            await systems.AssertSystemDoesNotExist(systemName);

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