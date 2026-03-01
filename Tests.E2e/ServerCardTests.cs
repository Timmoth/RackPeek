using Microsoft.Playwright;
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

    [Fact]
    public async Task User_Can_Add_And_Remove_Labels_From_Server_Card()
    {
        var (context, page) = await CreatePageAsync();
        var name = $"e2e-srv-lbl-{Guid.NewGuid():N}"[..16];

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

            var labels = card.Labels;

            await labels.AddLabelAsync("server", "env", "production");
            await labels.AssertLabelVisibleAsync("server", "env");

            await labels.AddLabelAsync("server", "owner", "team-a");
            await labels.AssertLabelVisibleAsync("server", "owner");

            await labels.RemoveLabelAsync("server", "owner");
            await labels.AssertLabelNotVisibleAsync("server", "owner");
            await labels.AssertLabelVisibleAsync("server", "env");

            await page.ReloadAsync();
            await labels.AssertLabelVisibleAsync("server", "env");
            await labels.AssertLabelNotVisibleAsync("server", "owner");

            await context.CloseAsync();
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task User_Can_Edit_Label_From_Server_Card()
    {
        var (context, page) = await CreatePageAsync();
        var name = $"e2e-srv-edit-{Guid.NewGuid():N}"[..16];

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

            var labels = card.Labels;

            await labels.AddLabelAsync("server", "env", "production");
            await labels.AssertLabelDisplaysAsync("server", "env", "production");

            await labels.EditLabelAsync("server", "env", "env", "staging");
            await labels.AssertLabelDisplaysAsync("server", "env", "staging");

            await page.ReloadAsync();
            await labels.AssertLabelDisplaysAsync("server", "env", "staging");

            await labels.EditLabelAsync("server", "env", "environment", "staging");
            await labels.AssertLabelNotVisibleAsync("server", "env");
            await labels.AssertLabelDisplaysAsync("server", "environment", "staging");

            await page.ReloadAsync();
            await labels.AssertLabelDisplaysAsync("server", "environment", "staging");

            await context.CloseAsync();
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task User_Can_Copy_Server_As_Template()
    {
        var context = await fixture.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            Permissions = ["clipboard-read", "clipboard-write"]
        });
        var page = await context.NewPageAsync();

        page.Console += (_, msg) =>
            _output.WriteLine($"[BrowserConsole] {msg.Type}: {msg.Text}");
        page.PageError += (_, msg) =>
            _output.WriteLine($"[PageError] {msg}");

        var name = $"e2e-srv-tpl-{Guid.NewGuid():N}"[..16];

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

            var officialName = "Official-Server-Template";

            // Button should be visible
            await Assertions.Expect(card.CopyAsTemplateButton(name)).ToBeVisibleAsync();
            await Assertions.Expect(card.CopyAsTemplateButton(name)).ToHaveTextAsync("Copy as Template");

            // Click the button — opens the template name modal
            await card.CopyAsTemplateButton(name).ClickAsync();

            // Modal should appear
            await Assertions.Expect(card.CopyTemplateModal).ToBeVisibleAsync();

            // Fill in the official hardware name and submit
            await card.CopyTemplateInput.FillAsync(officialName);
            await card.CopyTemplateSubmit.ClickAsync();

            // Button text should change to "Copied!"
            await Assertions.Expect(card.CopyAsTemplateButton(name)).ToHaveTextAsync("Copied!");

            // Verify clipboard contains valid template YAML with the official name
            var clipboardText = await page.EvaluateAsync<string>("() => navigator.clipboard.readText()");

            Assert.Contains("kind: Server", clipboardText);
            Assert.Contains($"name: {officialName}", clipboardText);
            Assert.DoesNotContain($"name: {name}", clipboardText);
            Assert.DoesNotContain("tags:", clipboardText);
            Assert.DoesNotContain("labels:", clipboardText);
            Assert.DoesNotContain("runsOn:", clipboardText);

            // After delay, button text should revert
            await page.WaitForTimeoutAsync(2500);
            await Assertions.Expect(card.CopyAsTemplateButton(name)).ToHaveTextAsync("Copy as Template");
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
}
