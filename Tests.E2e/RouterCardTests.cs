using Microsoft.Playwright;
using Tests.E2e.Infra;
using Tests.E2e.PageObjectModels;
using Xunit.Abstractions;

namespace Tests.E2e;

public class RouterCardTests(
    PlaywrightFixture fixture,
    ITestOutputHelper output) : E2ETestBase(fixture, output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public async Task User_Can_Edit_Router_Model_And_Features()
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
            await hardwareTree.GotoRoutersListAsync();

            var list = new RouterListPom(page);
            await list.AssertLoadedAsync();

            await list.AddRouterAsync(name);
            await list.AssertRouterExists(name);

            // Go to details page so we can exercise the card component
            await list.OpenRouterAsync(name);

            var card = new RouterCardPom(page);

            await Assertions.Expect(card.RouterItem(name)).ToBeVisibleAsync();

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
    public async Task User_Can_Rename_Router_From_Details_Page()
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
            await hardwareTree.GotoRoutersListAsync();

            var list = new RouterListPom(page);
            await list.AssertLoadedAsync();

            await list.AddRouterAsync(name);
            await list.OpenRouterAsync(name);

            var card = new RouterCardPom(page);

            await card.RenameRouterAsync(name, renamed);

            // after rename we should be on new URL and see the renamed card root
            await Assertions.Expect(card.RouterItem(renamed)).ToBeVisibleAsync();
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
    public async Task User_Can_Clone_Router_From_Details_Page()
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
            await hardwareTree.GotoRoutersListAsync();

            var list = new RouterListPom(page);
            await list.AssertLoadedAsync();

            await list.AddRouterAsync(name);
            await list.OpenRouterAsync(name);

            var card = new RouterCardPom(page);

            await card.CloneRouterAsync(name, clone);

            // should be on the clone's details URL and see the clone card
            await Assertions.Expect(card.RouterItem(clone)).ToBeVisibleAsync();
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
    public async Task User_Can_Delete_Router_From_Details_Page()
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
            await hardwareTree.GotoRoutersListAsync();

            var list = new RouterListPom(page);
            await list.AssertLoadedAsync();

            await list.AddRouterAsync(name);
            await list.OpenRouterAsync(name);

            var card = new RouterCardPom(page);

            await card.DeleteRouterAsync(name);

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
    public async Task User_Can_Add_And_Remove_Tags_From_Router_Card()
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
            await hardwareTree.GotoRoutersListAsync();

            var list = new RouterListPom(page);
            await list.AssertLoadedAsync();

            await list.AddRouterAsync(name);
            await page.WaitForURLAsync($"**/resources/hardware/{name}");

            var card = new RouterCardPom(page);
            var tags = card.Tags;

            // -------------------------------------------------
            // Add multiple tags in one modal interaction
            // -------------------------------------------------

            await tags.AddTagsAsync("router", "Foo", "Bar", "Baz");

            await tags.AssertTagVisibleAsync("router", "Foo");
            await tags.AssertTagVisibleAsync("router", "Bar");
            await tags.AssertTagVisibleAsync("router", "Baz");

            // -------------------------------------------------
            // Remove a single tag
            // -------------------------------------------------

            await tags.RemoveTagAsync("router", "Bar");

            await tags.AssertTagNotVisibleAsync("router", "Bar");
            await tags.AssertTagVisibleAsync("router", "Foo");
            await tags.AssertTagVisibleAsync("router", "Baz");

            // -------------------------------------------------
            // Reload to verify persistence
            // -------------------------------------------------

            await page.ReloadAsync();

            await tags.AssertTagVisibleAsync("router", "Foo");
            await tags.AssertTagVisibleAsync("router", "Baz");
            await tags.AssertTagNotVisibleAsync("router", "Bar");

            await context.CloseAsync();
        }
        finally
        {
            await context.CloseAsync();
        }
    }
}
