using Microsoft.Playwright;
using Tests.E2e.Infra;
using Tests.E2e.PageObjectModels;
using Xunit.Abstractions;

namespace Tests.E2e;

public class MainLayoutTests(
    PlaywrightFixture fixture,
    ITestOutputHelper output) : E2ETestBase(fixture, output) {
    private readonly PlaywrightFixture _fixture = fixture;

    [Fact]
    public async Task Mobile_early_access_banner_links_to_the_discord_invite() {
        (IBrowserContext context, IPage page) = await CreatePageAsync();

        try {
            await page.GotoAsync(_fixture.BaseUrl);
            var layout = new MainLayoutPom(page);
            await layout.AssertLoadedAsync();

            await Assertions.Expect(layout.MobileEarlyAccess).ToBeVisibleAsync();
            await Assertions.Expect(layout.MobileEarlyAccess).ToHaveAttributeAsync(
                "href",
                "https://discord.gg/egXRPdesee");
            await Assertions.Expect(layout.MobileEarlyAccess).ToHaveAttributeAsync(
                "target",
                "_blank");
        }
        finally {
            await context.CloseAsync();
        }
    }
}
