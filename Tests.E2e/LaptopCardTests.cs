using Microsoft.Playwright;
using Tests.E2e.Infra;
using Tests.E2e.PageObjectModels;
using Xunit.Abstractions;

namespace Tests.E2e;

public class LaptopCardTests(
    PlaywrightFixture fixture,
    ITestOutputHelper output) : E2ETestBase(fixture, output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public async Task User_Can_Add_And_Delete_Laptop()
    {
        var (context, page) = await CreatePageAsync();
        var laptopName = $"e2e-dt-{Guid.NewGuid():N}"[..16];

        try
        {
            await page.GotoAsync(fixture.BaseUrl);

            var layout = new MainLayoutPom(page);
            await layout.AssertLoadedAsync();
            await layout.GotoHardwareAsync();

            var hardwarePage = new HardwareTreePom(page);
            await hardwarePage.AssertLoadedAsync();
            await hardwarePage.GotoLaptopsListAsync();

            var listPage = new LaptopListPom(page);
            await listPage.AssertLoadedAsync();

            await listPage.AddLaptopAsync(laptopName);

            // creation should navigate to details page
            await page.WaitForURLAsync($"**/resources/hardware/{laptopName}");

            // delete from details page (card)
            var card = new LaptopCardPom(page);
            await Assertions.Expect(card.LaptopItem(laptopName)).ToBeVisibleAsync();
            await card.DeleteLaptopAsync(laptopName);

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
    public async Task User_Can_Rename_Laptop_From_Details_Page()
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
            await hardwarePage.GotoLaptopsListAsync();

            var listPage = new LaptopListPom(page);
            await listPage.AssertLoadedAsync();

            await listPage.AddLaptopAsync(originalName);
            await page.WaitForURLAsync($"**/resources/hardware/{originalName}");

            var card = new LaptopCardPom(page);
            await Assertions.Expect(card.LaptopItem(originalName)).ToBeVisibleAsync();

            await card.RenameLaptopAsync(originalName, renamedName);
            await Assertions.Expect(card.LaptopItem(renamedName)).ToBeVisibleAsync();

            // cleanup
            await card.DeleteLaptopAsync(renamedName);
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
    public async Task User_Can_Clone_Laptop_From_Details_Page()
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
            await hardwarePage.GotoLaptopsListAsync();

            var listPage = new LaptopListPom(page);
            await listPage.AssertLoadedAsync();

            await listPage.AddLaptopAsync(originalName);
            await page.WaitForURLAsync($"**/resources/hardware/{originalName}");

            var card = new LaptopCardPom(page);
            await Assertions.Expect(card.LaptopItem(originalName)).ToBeVisibleAsync();

            await card.CloneLaptopAsync(originalName, cloneName);
            await Assertions.Expect(card.LaptopItem(cloneName)).ToBeVisibleAsync();

            // cleanup: delete clone then original
            await card.DeleteLaptopAsync(cloneName);
            await page.WaitForURLAsync("**/hardware/tree");

            // go back to original and delete it too
            await page.GotoAsync($"{fixture.BaseUrl}/resources/hardware/{originalName}");
            await Assertions.Expect(card.LaptopItem(originalName)).ToBeVisibleAsync();
            await card.DeleteLaptopAsync(originalName);
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
    public async Task User_Can_Edit_Laptop_Notes_And_Save()
    {
        var (context, page) = await CreatePageAsync();
        var laptopName = $"e2e-dt-{Guid.NewGuid():N}"[..16];
        var notes = $"notes-{Guid.NewGuid():N}";

        try
        {
            await page.GotoAsync(fixture.BaseUrl);

            var layout = new MainLayoutPom(page);
            await layout.AssertLoadedAsync();
            await layout.GotoHardwareAsync();

            var hardwarePage = new HardwareTreePom(page);
            await hardwarePage.AssertLoadedAsync();
            await hardwarePage.GotoLaptopsListAsync();

            var listPage = new LaptopListPom(page);
            await listPage.AssertLoadedAsync();

            await listPage.AddLaptopAsync(laptopName);
            await page.WaitForURLAsync($"**/resources/hardware/{laptopName}");

            var card = new LaptopCardPom(page);
            await Assertions.Expect(card.LaptopItem(laptopName)).ToBeVisibleAsync();

            // start editing notes via MarkdownViewer edit button
            await card.NotesViewerEditButton(laptopName).ClickAsync();

            // ensure editor visible then fill + save
            await Assertions.Expect(card.NotesEditorRoot(laptopName)).ToBeVisibleAsync();
            await card.NotesEditorTextarea(laptopName).FillAsync(notes);
            await card.NotesEditorSave(laptopName).ClickAsync();

            // viewer back, and content should contain the notes
            await Assertions.Expect(card.NotesViewerRoot(laptopName)).ToBeVisibleAsync();
            await Assertions.Expect(card.NotesViewerRoot(laptopName)).ToContainTextAsync(notes);

            // cleanup
            await card.DeleteLaptopAsync(laptopName);
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
    public async Task User_Can_Edit_Laptop_Notes_And_Cancel()
    {
        var (context, page) = await CreatePageAsync();
        var laptopName = $"e2e-dt-{Guid.NewGuid():N}"[..16];
        var notes = $"notes-{Guid.NewGuid():N}";

        try
        {
            await page.GotoAsync(fixture.BaseUrl);

            var layout = new MainLayoutPom(page);
            await layout.AssertLoadedAsync();
            await layout.GotoHardwareAsync();

            var hardwarePage = new HardwareTreePom(page);
            await hardwarePage.AssertLoadedAsync();
            await hardwarePage.GotoLaptopsListAsync();

            var listPage = new LaptopListPom(page);
            await listPage.AssertLoadedAsync();

            await listPage.AddLaptopAsync(laptopName);
            await page.WaitForURLAsync($"**/resources/hardware/{laptopName}");

            var card = new LaptopCardPom(page);
            await Assertions.Expect(card.LaptopItem(laptopName)).ToBeVisibleAsync();

            await card.NotesViewerEditButton(laptopName).ClickAsync();
            await Assertions.Expect(card.NotesEditorRoot(laptopName)).ToBeVisibleAsync();

            await card.NotesEditorTextarea(laptopName).FillAsync(notes);
            await card.NotesEditorCancel(laptopName).ClickAsync();

            // viewer should be back, and should NOT show the cancelled notes
            await Assertions.Expect(card.NotesViewerRoot(laptopName)).ToBeVisibleAsync();
            await Assertions.Expect(card.NotesViewerRoot(laptopName)).Not.ToContainTextAsync(notes);

            // cleanup
            await card.DeleteLaptopAsync(laptopName);
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
    public async Task User_Can_Add_And_Remove_Tags_From_Laptop_Card()
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
            await hardwareTree.GotoLaptopsListAsync();

            var list = new LaptopListPom(page);
            await list.AssertLoadedAsync();

            await list.AddLaptopAsync(name);
            await page.WaitForURLAsync($"**/resources/hardware/{name}");

            var card = new LaptopCardPom(page);
            await Assertions.Expect(card.LaptopItem(name)).ToBeVisibleAsync();

            var tags = card.Tags;

            // -------------------------------------------------
            // Add multiple tags in one modal interaction
            // -------------------------------------------------

            await tags.AddTagsAsync("laptop", "Foo", "Bar", "Baz");

            await tags.AssertTagVisibleAsync("laptop", "Foo");
            await tags.AssertTagVisibleAsync("laptop", "Bar");
            await tags.AssertTagVisibleAsync("laptop", "Baz");

            // -------------------------------------------------
            // Remove a single tag
            // -------------------------------------------------

            await tags.RemoveTagAsync("laptop", "Bar");

            await tags.AssertTagNotVisibleAsync("laptop", "Bar");
            await tags.AssertTagVisibleAsync("laptop", "Foo");
            await tags.AssertTagVisibleAsync("laptop", "Baz");

            // -------------------------------------------------
            // Reload to verify persistence
            // -------------------------------------------------

            await page.ReloadAsync();

            await tags.AssertTagVisibleAsync("laptop", "Foo");
            await tags.AssertTagVisibleAsync("laptop", "Baz");
            await tags.AssertTagNotVisibleAsync("laptop", "Bar");

            await context.CloseAsync();
        }
        finally
        {
            await context.CloseAsync();
        }
    }
}
