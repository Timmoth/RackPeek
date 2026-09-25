using Microsoft.Playwright;
using Tests.E2e.Infra;
using Tests.E2e.PageObjectModels;
using Xunit.Abstractions;

namespace Tests.E2e;

/// <summary>
///     Blazor Server prerenders every page before the circuit attaches, and anything
///     typed into that static HTML never reaches the server-side model. A fast test —
///     or a fast typist on a slow link — can therefore submit the add form and be told
///     "name is required" despite having filled it in. The injected latency makes the
///     attach window, which CI runners only sometimes lose, wide enough to lose every
///     time; the page object must wait for the circuit before it types.
/// </summary>
public class AddResourceRaceTests(
    PlaywrightFixture fixture,
    ITestOutputHelper output) : E2ETestBase(fixture, output) {
    private readonly PlaywrightFixture _fixture = fixture;

    [Fact]
    public async Task Adding_A_Resource_Works_Before_The_Circuit_Has_Warmed_Up() {
        (IBrowserContext context, IPage page) = await CreatePageAsync();
        await BlazorLatency.AddAsync(page, TimeSpan.FromMilliseconds(300));

        var name = $"e2e-oth-{Guid.NewGuid():N}"[..16];

        try {
            await page.GotoAsync($"{_fixture.BaseUrl}/other/list");

            var list = new OtherListPom(page);
            await list.AddOtherAsync(name);
        }
        finally {
            await context.CloseAsync();
        }
    }
}
