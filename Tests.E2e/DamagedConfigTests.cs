using Microsoft.Playwright;
using Tests.E2e.Infra;
using Xunit.Abstractions;

namespace Tests.E2e;

/// <summary>
///     Web half of https://github.com/Timmoth/RackPeek/issues/337. A config that
///     exists but cannot be read must not leave the app stuck on "Loading…" — the
///     YAML editor is how someone repairs it — and must never be reported as an
///     empty inventory. These tests stage a damaged config in the container, then
///     repair it through the UI.
/// </summary>
public class DamagedConfigTests(
    PlaywrightFixture fixture,
    ITestOutputHelper output) : E2ETestBase(fixture, output) {
    private readonly PlaywrightFixture _fixture = fixture;
    private readonly ITestOutputHelper _output = output;

    // Cut mid-token, exactly as an interrupted in-place write would leave it.
    private const string _damaged = """
                                    version: 4
                                    resources:
                                    - kind: Server
                                      name: srv-a
                                    - ki
                                    """;

    private const string _healthy = """
                                    version: 4
                                    resources:
                                    - kind: Server
                                      name: repaired-srv
                                    connections: []
                                    """;

    [Fact]
    public async Task The_App_Still_Loads_And_Can_Repair_A_Damaged_Config() {
        (IBrowserContext context, IPage page) = await CreatePageAsync();

        try {
            await _fixture.WriteConfigAsync(_damaged);

            // 1. The app renders rather than hanging on "Loading…".
            await page.GotoAsync($"{_fixture.BaseUrl}/yaml");

            await Assertions.Expect(page.GetByTestId("circuit-probe"))
                .ToHaveAttributeAsync("data-circuit-ready", "true");

            // 2. The editor shows the damaged file, so it can be fixed in place.
            ILocator content = page.GetByTestId("yaml-file-content");
            await Assertions.Expect(content).ToBeVisibleAsync();
            await Assertions.Expect(content).ToContainTextAsync("srv-a");

            // 3. Repair it through the editor.
            await page.GetByRole(AriaRole.Button, new() { Name = "Edit" }).ClickAsync();

            ILocator textarea = page.Locator("textarea");
            await Assertions.Expect(textarea).ToBeVisibleAsync();
            await textarea.FillAsync(_healthy);

            await page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

            await Assertions.Expect(page.GetByTestId("yaml-file-error")).ToHaveCountAsync(0);

            // 4. The inventory reads correctly again.
            await page.GotoAsync($"{_fixture.BaseUrl}/servers/list");
            await Assertions.Expect(page.GetByText("repaired-srv").First).ToBeVisibleAsync();

            Assert.Contains("repaired-srv", await _fixture.ReadConfigAsync());
        }
        catch (Exception) {
            _output.WriteLine($"TEST FAILED — URL: {page.Url}");
            _output.WriteLine(await page.ContentAsync());
            throw;
        }
        finally {
            // Leave the container usable for any other test in this class.
            await _fixture.WriteConfigAsync(_healthy);
            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task A_Damaged_Config_Is_Never_Reported_As_An_Empty_Inventory() {
        (IBrowserContext context, IPage page) = await CreatePageAsync();

        try {
            await _fixture.WriteConfigAsync(_damaged);

            await page.GotoAsync($"{_fixture.BaseUrl}/servers/list");

            // The inventory pages read through the collection, which now refuses a
            // config it could not parse. What must never happen is the page
            // rendering a confident, empty list over a recoverable file.
            var body = await page.InnerTextAsync("body");
            Assert.DoesNotContain("srv-a", body);
            Assert.DoesNotContain("No servers", body, StringComparison.OrdinalIgnoreCase);

            // And the damaged file is still on disk, untouched by the failed read.
            Assert.Equal(_damaged, (await _fixture.ReadConfigAsync()).TrimEnd('\n'));
        }
        catch (Exception) {
            _output.WriteLine($"TEST FAILED — URL: {page.Url}");
            _output.WriteLine(await page.ContentAsync());
            throw;
        }
        finally {
            await _fixture.WriteConfigAsync(_healthy);
            await context.CloseAsync();
        }
    }
}
