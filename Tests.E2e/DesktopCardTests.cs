using Microsoft.Playwright;
using Tests.E2e.Infra;
using Tests.E2e.PageObjectModels;
using Xunit.Abstractions;

namespace Tests.E2e;

public class DesktopCardTests(
    PlaywrightFixture fixture,
    ITestOutputHelper output) : E2ETestBase(fixture, output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public async Task User_Can_Add_And_Delete_Desktop()
    {
        var (context, page) = await CreatePageAsync();
        var desktopName = $"e2e-dt-{Guid.NewGuid():N}"[..16];

        try
        {
            await page.GotoAsync(fixture.BaseUrl);

            var layout = new MainLayoutPom(page);
            await layout.AssertLoadedAsync();
            await layout.GotoHardwareAsync();

            var hardwarePage = new HardwareTreePom(page);
            await hardwarePage.AssertLoadedAsync();
            await hardwarePage.GotoDesktopsListAsync();

            var listPage = new DesktopsListPom(page);
            await listPage.AssertLoadedAsync();

            await listPage.AddDesktopAsync(desktopName);

            // creation should navigate to details page
            await page.WaitForURLAsync($"**/resources/hardware/{desktopName}");

            // delete from details page (card)
            var card = new DesktopCardPom(page);
            await Assertions.Expect(card.DesktopItem(desktopName)).ToBeVisibleAsync();
            await card.DeleteDesktopAsync(desktopName);

            // after deletion you redirect (your page does Nav.NavigateTo("/hardware/tree"))
            await page.WaitForURLAsync("**/hardware/tree");
        }
        catch (Exception)
        {
            _output.WriteLine("TEST FAILED — Capturing diagnostics");
            _output.WriteLine($"Current URL: {page.Url}");
            _output.WriteLine("==== DOM SNAPSHOT START ====");
            _output.WriteLine(await page.ContentAsync());
            _output.WriteLine("==== DOM SNAPSHOT END ====");
            throw;
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task User_Can_Rename_Desktop_From_Details_Page()
    {
        var (context, page) = await CreatePageAsync();
        var originalName = $"e2e-dt-{Guid.NewGuid():N}"[..16];
        var renamedName = $"e2e-dt-{Guid.NewGuid():N}"[..16];

        try
        {
            await page.GotoAsync(fixture.BaseUrl);

            var layout = new MainLayoutPom(page);
            await layout.AssertLoadedAsync();
            await layout.GotoHardwareAsync();

            var hardwarePage = new HardwareTreePom(page);
            await hardwarePage.AssertLoadedAsync();
            await hardwarePage.GotoDesktopsListAsync();

            var listPage = new DesktopsListPom(page);
            await listPage.AssertLoadedAsync();

            await listPage.AddDesktopAsync(originalName);
            await page.WaitForURLAsync($"**/resources/hardware/{originalName}");

            var card = new DesktopCardPom(page);
            await Assertions.Expect(card.DesktopItem(originalName)).ToBeVisibleAsync();

            await card.RenameDesktopAsync(originalName, renamedName);
            await Assertions.Expect(card.DesktopItem(renamedName)).ToBeVisibleAsync();

            // cleanup
            await card.DeleteDesktopAsync(renamedName);
            await page.WaitForURLAsync("**/hardware/tree");
        }
        catch (Exception)
        {
            _output.WriteLine("TEST FAILED — Capturing diagnostics");
            _output.WriteLine($"Current URL: {page.Url}");
            _output.WriteLine("==== DOM SNAPSHOT START ====");
            _output.WriteLine(await page.ContentAsync());
            _output.WriteLine("==== DOM SNAPSHOT END ====");
            throw;
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task User_Can_Clone_Desktop_From_Details_Page()
    {
        var (context, page) = await CreatePageAsync();
        var originalName = $"e2e-dt-{Guid.NewGuid():N}"[..16];
        var cloneName = $"e2e-dt-{Guid.NewGuid():N}"[..16];

        try
        {
            await page.GotoAsync(fixture.BaseUrl);

            var layout = new MainLayoutPom(page);
            await layout.AssertLoadedAsync();
            await layout.GotoHardwareAsync();

            var hardwarePage = new HardwareTreePom(page);
            await hardwarePage.AssertLoadedAsync();
            await hardwarePage.GotoDesktopsListAsync();

            var listPage = new DesktopsListPom(page);
            await listPage.AssertLoadedAsync();

            await listPage.AddDesktopAsync(originalName);
            await page.WaitForURLAsync($"**/resources/hardware/{originalName}");

            var card = new DesktopCardPom(page);
            await Assertions.Expect(card.DesktopItem(originalName)).ToBeVisibleAsync();

            await card.CloneDesktopAsync(originalName, cloneName);
            await Assertions.Expect(card.DesktopItem(cloneName)).ToBeVisibleAsync();

            // cleanup: delete clone then original
            await card.DeleteDesktopAsync(cloneName);
            await page.WaitForURLAsync("**/hardware/tree");

            // go back to original and delete it too
            await page.GotoAsync($"{fixture.BaseUrl}/resources/hardware/{originalName}");
            await Assertions.Expect(card.DesktopItem(originalName)).ToBeVisibleAsync();
            await card.DeleteDesktopAsync(originalName);
            await page.WaitForURLAsync("**/hardware/tree");
        }
        catch (Exception)
        {
            _output.WriteLine("TEST FAILED — Capturing diagnostics");
            _output.WriteLine($"Current URL: {page.Url}");
            _output.WriteLine("==== DOM SNAPSHOT START ====");
            _output.WriteLine(await page.ContentAsync());
            _output.WriteLine("==== DOM SNAPSHOT END ====");
            throw;
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task User_Can_Edit_Desktop_Notes_And_Save()
    {
        var (context, page) = await CreatePageAsync();
        var desktopName = $"e2e-dt-{Guid.NewGuid():N}"[..16];
        var notes = $"notes-{Guid.NewGuid():N}";

        try
        {
            await page.GotoAsync(fixture.BaseUrl);

            var layout = new MainLayoutPom(page);
            await layout.AssertLoadedAsync();
            await layout.GotoHardwareAsync();

            var hardwarePage = new HardwareTreePom(page);
            await hardwarePage.AssertLoadedAsync();
            await hardwarePage.GotoDesktopsListAsync();

            var listPage = new DesktopsListPom(page);
            await listPage.AssertLoadedAsync();

            await listPage.AddDesktopAsync(desktopName);
            await page.WaitForURLAsync($"**/resources/hardware/{desktopName}");

            var card = new DesktopCardPom(page);
            await Assertions.Expect(card.DesktopItem(desktopName)).ToBeVisibleAsync();

            // start editing notes via MarkdownViewer edit button
            await card.NotesViewerEditButton(desktopName).ClickAsync();

            // ensure editor visible then fill + save
            await Assertions.Expect(card.NotesEditorRoot(desktopName)).ToBeVisibleAsync();
            await card.NotesEditorTextarea(desktopName).FillAsync(notes);
            await card.NotesEditorSave(desktopName).ClickAsync();

            // viewer back, and content should contain the notes
            await Assertions.Expect(card.NotesViewerRoot(desktopName)).ToBeVisibleAsync();
            await Assertions.Expect(card.NotesViewerRoot(desktopName)).ToContainTextAsync(notes);

            // cleanup
            await card.DeleteDesktopAsync(desktopName);
            await page.WaitForURLAsync("**/hardware/tree");
        }
        catch (Exception)
        {
            _output.WriteLine("TEST FAILED — Capturing diagnostics");
            _output.WriteLine($"Current URL: {page.Url}");
            _output.WriteLine("==== DOM SNAPSHOT START ====");
            _output.WriteLine(await page.ContentAsync());
            _output.WriteLine("==== DOM SNAPSHOT END ====");
            throw;
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task User_Can_Edit_Desktop_Notes_And_Cancel()
    {
        var (context, page) = await CreatePageAsync();
        var desktopName = $"e2e-dt-{Guid.NewGuid():N}"[..16];
        var notes = $"notes-{Guid.NewGuid():N}";

        try
        {
            await page.GotoAsync(fixture.BaseUrl);

            var layout = new MainLayoutPom(page);
            await layout.AssertLoadedAsync();
            await layout.GotoHardwareAsync();

            var hardwarePage = new HardwareTreePom(page);
            await hardwarePage.AssertLoadedAsync();
            await hardwarePage.GotoDesktopsListAsync();

            var listPage = new DesktopsListPom(page);
            await listPage.AssertLoadedAsync();

            await listPage.AddDesktopAsync(desktopName);
            await page.WaitForURLAsync($"**/resources/hardware/{desktopName}");

            var card = new DesktopCardPom(page);
            await Assertions.Expect(card.DesktopItem(desktopName)).ToBeVisibleAsync();

            await card.NotesViewerEditButton(desktopName).ClickAsync();
            await Assertions.Expect(card.NotesEditorRoot(desktopName)).ToBeVisibleAsync();

            await card.NotesEditorTextarea(desktopName).FillAsync(notes);
            await card.NotesEditorCancel(desktopName).ClickAsync();

            // viewer should be back, and should NOT show the cancelled notes
            await Assertions.Expect(card.NotesViewerRoot(desktopName)).ToBeVisibleAsync();
            await Assertions.Expect(card.NotesViewerRoot(desktopName)).Not.ToContainTextAsync(notes);

            // cleanup
            await card.DeleteDesktopAsync(desktopName);
            await page.WaitForURLAsync("**/hardware/tree");
        }
        catch (Exception)
        {
            _output.WriteLine("TEST FAILED — Capturing diagnostics");
            _output.WriteLine($"Current URL: {page.Url}");
            _output.WriteLine("==== DOM SNAPSHOT START ====");
            _output.WriteLine(await page.ContentAsync());
            _output.WriteLine("==== DOM SNAPSHOT END ====");
            throw;
        }
        finally
        {
            await context.CloseAsync();
        }
    }
    
    
    [Fact]
    public async Task User_Can_Add_And_Remove_Tags_From_Desktop_Card()
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
            await hardwareTree.GotoDesktopsListAsync();

            var list = new DesktopsListPom(page);
            await list.AssertLoadedAsync();

            await list.AddDesktopAsync(name);
            await page.WaitForURLAsync($"**/resources/hardware/{name}");

            var card = new DesktopCardPom(page);
            await Assertions.Expect(card.DesktopItem(name)).ToBeVisibleAsync();

            var tags = card.Tags;

            // -------------------------------------------------
            // Add multiple tags in one modal interaction
            // -------------------------------------------------

            await tags.AddTagsAsync("desktop", "Foo", "Bar", "Baz");

            await tags.AssertTagVisibleAsync("desktop", "Foo");
            await tags.AssertTagVisibleAsync("desktop", "Bar");
            await tags.AssertTagVisibleAsync("desktop", "Baz");

            // -------------------------------------------------
            // Remove a single tag
            // -------------------------------------------------

            await tags.RemoveTagAsync("desktop", "Bar");

            await tags.AssertTagNotVisibleAsync("desktop", "Bar");
            await tags.AssertTagVisibleAsync("desktop", "Foo");
            await tags.AssertTagVisibleAsync("desktop", "Baz");

            // -------------------------------------------------
            // Reload to verify persistence
            // -------------------------------------------------

            await page.ReloadAsync();

            await tags.AssertTagVisibleAsync("desktop", "Foo");
            await tags.AssertTagVisibleAsync("desktop", "Baz");
            await tags.AssertTagNotVisibleAsync("desktop", "Bar");

            await context.CloseAsync();
        }
        finally
        {
            await context.CloseAsync();
        }
    }
    
    
}
