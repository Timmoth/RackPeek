using Tests.E2e.Infra;
using Tests.E2e.PageObjectModels;
using Xunit.Abstractions;

namespace Tests.E2e;

public class ServerCardTests(
    PlaywrightFixture fixture,
    ITestOutputHelper output) : E2ETestBase(fixture, output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public async Task User_Can_Rename_Clone_And_Delete_Server_From_Details_Page()
    {
        var (context, page) = await CreatePageAsync();

        var originalName = $"e2e-srv-{Guid.NewGuid():N}"[..16];
        var renamedName  = $"e2e-srv-rn-{Guid.NewGuid():N}"[..16];
        var cloneName    = $"e2e-srv-cl-{Guid.NewGuid():N}"[..16];

        try
        {
            // ------------------------------------
            // Navigate to Servers list
            // ------------------------------------
            await page.GotoAsync(fixture.BaseUrl);

            var layout = new MainLayoutPom(page);
            await layout.AssertLoadedAsync();
            await layout.GotoHardwareAsync();

            var hardwareTree = new HardwareTreePom(page);
            await hardwareTree.AssertLoadedAsync();
            await hardwareTree.GotoServersListAsync();

            var listPage = new ServersListPom(page);
            await listPage.AssertLoadedAsync();

            // ------------------------------------
            // Create server
            // ------------------------------------
            await listPage.AddServerAsync(originalName);

            // If list does not auto-navigate, open it
            if (!page.Url.Contains($"/resources/hardware/{originalName}", StringComparison.OrdinalIgnoreCase))
            {
                await listPage.OpenServerAsync(originalName);
            }

            var card = new ServerCardPom(page);
            await card.AssertVisibleAsync(originalName);

            // ====================================
            // RENAME
            // ====================================
            await card.RenameAsync(originalName, renamedName);

            await card.AssertVisibleAsync(renamedName);

            // ====================================
            // CLONE
            // ====================================
            await card.CloneAsync(renamedName, cloneName);

            await card.AssertVisibleAsync(cloneName);

            // ====================================
            // DELETE CLONE
            // ====================================
            await card.DeleteAsync(cloneName);

            // Details page delete navigates to tree
            await page.WaitForURLAsync("**/hardware/tree");

            // ====================================
            // DELETE RENAMED ORIGINAL
            // ====================================
            await page.GotoAsync($"{fixture.BaseUrl}/resources/hardware/{renamedName}");

            await card.AssertVisibleAsync(renamedName);

            await card.DeleteAsync(renamedName);

            await page.WaitForURLAsync("**/hardware/tree");
        }
        catch (Exception)
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
    
    
    [Fact]
    public async Task User_Can_Add_And_Remove_Tags_From_Server_Card()
    {
        var (context, page) = await CreatePageAsync();
        var name = $"e2e-ap-{Guid.NewGuid():N}"[..16];

        try
        {
            await page.GotoAsync(fixture.BaseUrl);

            var layout = new MainLayoutPom(page);
            await layout.AssertLoadedAsync();
            await layout.GotoHardwareAsync();

            var hardwareTree = new HardwareTreePom(page);
            await hardwareTree.AssertLoadedAsync();
            await hardwareTree.GotoServersListAsync();

            var list = new ServersListPom(page);
            await list.AssertLoadedAsync();

            await list.AddServerAsync(name);
            await page.WaitForURLAsync($"**/resources/hardware/{name}");

            var card = new ServerCardPom(page);
            await card.AssertVisibleAsync(name);

            var tags = card.Tags;

            // -------------------------------------------------
            // Add multiple tags in one modal interaction
            // -------------------------------------------------

            await tags.AddTagsAsync("server", "Foo", "Bar", "Baz");

            await tags.AssertTagVisibleAsync("server", "Foo");
            await tags.AssertTagVisibleAsync("server", "Bar");
            await tags.AssertTagVisibleAsync("server", "Baz");

            // -------------------------------------------------
            // Remove a single tag
            // -------------------------------------------------

            await tags.RemoveTagAsync("server", "Bar");

            await tags.AssertTagNotVisibleAsync("server", "Bar");
            await tags.AssertTagVisibleAsync("server", "Foo");
            await tags.AssertTagVisibleAsync("server", "Baz");

            // -------------------------------------------------
            // Reload to verify persistence
            // -------------------------------------------------

            await page.ReloadAsync();

            await tags.AssertTagVisibleAsync("server", "Foo");
            await tags.AssertTagVisibleAsync("server", "Baz");
            await tags.AssertTagNotVisibleAsync("server", "Bar");

            await context.CloseAsync();
        }
        finally
        {
            await context.CloseAsync();
        }
    }
}
