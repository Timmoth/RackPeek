using Microsoft.Playwright;
using Tests.E2e.Infra;
using Tests.E2e.PageObjectModels;
using Xunit.Abstractions;

namespace Tests.E2e;

public class SwitchCardTests(
    PlaywrightFixture fixture,
    ITestOutputHelper output) : E2ETestBase(fixture, output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public async Task User_Can_Edit_Switch_Model_And_Features()
    {
        var (context, page) = await CreatePageAsync();
        var name = $"e2e-fw-{Guid.NewGuid():N}"[..16];

        try
        {
            await page.GotoAsync(fixture.BaseUrl);

            var layout = new MainLayoutPom(page);
            await layout.AssertLoadedAsync();
            await layout.GotoHardwareAsync();

            var hardwareTree = new HardwareTreePom(page);
            await hardwareTree.AssertLoadedAsync();
            await hardwareTree.GotoSwitchesListAsync();

            var list = new SwitchListPom(page);
            await list.AssertLoadedAsync();

            await list.AddSwitchAsync(name);
            await list.AssertSwitchExists(name);

            // Go to details page so we can exercise the card component
            await list.OpenSwitchAsync(name);

            var card = new SwitchCardPom(page);

            await Assertions.Expect(card.SwitchItem(name)).ToBeVisibleAsync();

            // Enter edit mode
            await card.EnterEditModeAsync(name);

            // Update Model + toggle features
            var newModel = "e2e-model-123";
            await card.ModelInput(name).FillAsync(newModel);

            // Ensure both toggles are ON (idempotent)
            if (!await card.ManagedCheckbox(name).IsCheckedAsync())
                await card.ManagedCheckbox(name).CheckAsync();
            if (!await card.PoeCheckbox(name).IsCheckedAsync())
                await card.PoeCheckbox(name).CheckAsync();

            await card.SaveEditsAsync(name);

            // Validate view mode reflects changes
            await Assertions.Expect(card.ModelValue(name)).ToHaveTextAsync(newModel);
            await Assertions.Expect(card.ManagedBadge(name)).ToBeVisibleAsync();
            await Assertions.Expect(card.PoeBadge(name)).ToBeVisibleAsync();
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
    public async Task User_Can_Rename_Switch_From_Details_Page()
    {
        var (context, page) = await CreatePageAsync();
        var name = $"e2e-fw-{Guid.NewGuid():N}"[..16];
        var renamed = $"{name}-ren";

        try
        {
            await page.GotoAsync(fixture.BaseUrl);

            var layout = new MainLayoutPom(page);
            await layout.AssertLoadedAsync();
            await layout.GotoHardwareAsync();

            var hardwareTree = new HardwareTreePom(page);
            await hardwareTree.AssertLoadedAsync();
            await hardwareTree.GotoSwitchesListAsync();

            var list = new SwitchListPom(page);
            await list.AssertLoadedAsync();

            await list.AddSwitchAsync(name);
            await list.OpenSwitchAsync(name);

            var card = new SwitchCardPom(page);

            await card.RenameSwitchAsync(name, renamed);

            // after rename we should be on new URL and see the renamed card root
            await Assertions.Expect(card.SwitchItem(renamed)).ToBeVisibleAsync();
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
    public async Task User_Can_Clone_Switch_From_Details_Page()
    {
        var (context, page) = await CreatePageAsync();
        var name = $"e2e-fw-{Guid.NewGuid():N}"[..16];
        var clone = $"{name}-cpy";

        try
        {
            await page.GotoAsync(fixture.BaseUrl);

            var layout = new MainLayoutPom(page);
            await layout.AssertLoadedAsync();
            await layout.GotoHardwareAsync();

            var hardwareTree = new HardwareTreePom(page);
            await hardwareTree.AssertLoadedAsync();
            await hardwareTree.GotoSwitchesListAsync();

            var list = new SwitchListPom(page);
            await list.AssertLoadedAsync();

            await list.AddSwitchAsync(name);
            await list.OpenSwitchAsync(name);

            var card = new SwitchCardPom(page);

            await card.CloneSwitchAsync(name, clone);

            // should be on the clone's details URL and see the clone card
            await Assertions.Expect(card.SwitchItem(clone)).ToBeVisibleAsync();
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
    public async Task User_Can_Delete_Switch_From_Details_Page()
    {
        var (context, page) = await CreatePageAsync();
        var name = $"e2e-fw-{Guid.NewGuid():N}"[..16];

        try
        {
            await page.GotoAsync(fixture.BaseUrl);

            var layout = new MainLayoutPom(page);
            await layout.AssertLoadedAsync();
            await layout.GotoHardwareAsync();

            var hardwareTree = new HardwareTreePom(page);
            await hardwareTree.AssertLoadedAsync();
            await hardwareTree.GotoSwitchesListAsync();

            var list = new SwitchListPom(page);
            await list.AssertLoadedAsync();

            await list.AddSwitchAsync(name);
            await list.OpenSwitchAsync(name);

            var card = new SwitchCardPom(page);

            await card.DeleteSwitchAsync(name);

            // After delete, your page navigates away (Nav.NavigateTo("/hardware/tree"))
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
    public async Task User_Can_Add_And_Remove_Tags_From_Switch_Card()
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
            await hardwareTree.GotoSwitchesListAsync();

            var list = new SwitchListPom(page);
            await list.AssertLoadedAsync();

            await list.AddSwitchAsync(name);
            await page.WaitForURLAsync($"**/resources/hardware/{name}");

            var card = new SwitchCardPom(page);

            var tags = card.Tags;

            // -------------------------------------------------
            // Add multiple tags in one modal interaction
            // -------------------------------------------------

            await tags.AddTagsAsync("switch", "Foo", "Bar", "Baz");

            await tags.AssertTagVisibleAsync("switch", "Foo");
            await tags.AssertTagVisibleAsync("switch", "Bar");
            await tags.AssertTagVisibleAsync("switch", "Baz");

            // -------------------------------------------------
            // Remove a single tag
            // -------------------------------------------------

            await tags.RemoveTagAsync("switch", "Bar");

            await tags.AssertTagNotVisibleAsync("switch", "Bar");
            await tags.AssertTagVisibleAsync("switch", "Foo");
            await tags.AssertTagVisibleAsync("switch", "Baz");

            // -------------------------------------------------
            // Reload to verify persistence
            // -------------------------------------------------

            await page.ReloadAsync();

            await tags.AssertTagVisibleAsync("switch", "Foo");
            await tags.AssertTagVisibleAsync("switch", "Baz");
            await tags.AssertTagNotVisibleAsync("switch", "Bar");

            await context.CloseAsync();
        }
        finally
        {
            await context.CloseAsync();
        }
    }
}
