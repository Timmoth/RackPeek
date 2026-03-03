using Microsoft.Playwright;
using Tests.E2e.Infra;
using Tests.E2e.PageObjectModels;
using Xunit.Abstractions;

namespace Tests.E2e;

public class AnsibleInventoryTests(
    PlaywrightFixture fixture,
    ITestOutputHelper output) : E2ETestBase(fixture, output) {
    private readonly PlaywrightFixture _fixture = fixture;
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public async Task User_Can_Generate_Ansible_Inventory() {
        (IBrowserContext context, IPage page) = await CreatePageAsync();

        try {
            // Go home
            await page.GotoAsync(_fixture.BaseUrl);

            _output.WriteLine($"URL after Goto: {page.Url}");

            var layout = new MainLayoutPom(page);
            await layout.AssertLoadedAsync();

            // Navigate directly to inventory page
            await page.GotoAsync($"{_fixture.BaseUrl}/ansible/inventory");

            var inventoryPage = new AnsibleInventoryPagePom(page);
            await inventoryPage.AssertVisibleAsync();

            // Configure options
            await inventoryPage.SetGroupByTagsAsync("prod,staging");
            await inventoryPage.SetGroupByLabelsAsync("env");
            await inventoryPage.SetGlobalVarsAsync("""
                                                   ansible_user=ansible
                                                   ansible_python_interpreter=/usr/bin/python3
                                                   """);

            // Generate inventory
            await inventoryPage.GenerateAsync();

            // Assert output contains expected sections
            await inventoryPage.AssertInventoryContainsAsync("[all:vars]");
            await inventoryPage.AssertInventoryContainsAsync("ansible_user=ansible");

            // Ensure no warnings shown
            await inventoryPage.AssertNoWarningsAsync();

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
