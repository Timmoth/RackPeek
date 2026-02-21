using Microsoft.Playwright;
using Xunit.Abstractions;

namespace Tests.E2e.Infra;

public abstract class E2ETestBase(
    PlaywrightFixture fixture,
    ITestOutputHelper output) : IClassFixture<PlaywrightFixture>
{
    public async Task<(IBrowserContext, IPage)> CreatePageAsync()
    {
        var context = await fixture.Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        page.Console += (_, msg) =>
            output.WriteLine($"[BrowserConsole] {msg.Type}: {msg.Text}");

        page.PageError += (_, msg) =>
            output.WriteLine($"[PageError] {msg}");


        output.WriteLine($"BaseUrl: {fixture.BaseUrl}");

        return (context, page);
    }
}