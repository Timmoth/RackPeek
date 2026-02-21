using Microsoft.Playwright;
using Tests.E2e.Infra;
using Tests.E2e.PageObjectModels;
using Xunit.Abstractions;

namespace Tests.E2e;

public class FirewallCardTests(
    PlaywrightFixture fixture,
    ITestOutputHelper output) : E2ETestBase(fixture, output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public async Task User_Can_Edit_Firewall_Model_And_Features()
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
            await hardwareTree.GotoFirewallsListAsync();

            var list = new FirewallsListPom(page);
            await list.AssertLoadedAsync();

            await list.AddFirewallAsync(name);
            await list.AssertFirewallExists(name);

            // Go to details page so we can exercise the card component
            await list.OpenFirewallAsync(name);

            var card = new FirewallCardPom(page);

            await Assertions.Expect(card.FirewallItem(name)).ToBeVisibleAsync();

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
    public async Task User_Can_Rename_Firewall_From_Details_Page()
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
            await hardwareTree.GotoFirewallsListAsync();

            var list = new FirewallsListPom(page);
            await list.AssertLoadedAsync();

            await list.AddFirewallAsync(name);
            await list.OpenFirewallAsync(name);

            var card = new FirewallCardPom(page);

            await card.RenameFirewallAsync(name, renamed);

            // after rename we should be on new URL and see the renamed card root
            await Assertions.Expect(card.FirewallItem(renamed)).ToBeVisibleAsync();
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
    public async Task User_Can_Clone_Firewall_From_Details_Page()
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
            await hardwareTree.GotoFirewallsListAsync();

            var list = new FirewallsListPom(page);
            await list.AssertLoadedAsync();

            await list.AddFirewallAsync(name);
            await list.OpenFirewallAsync(name);

            var card = new FirewallCardPom(page);

            await card.CloneFirewallAsync(name, clone);

            // should be on the clone's details URL and see the clone card
            await Assertions.Expect(card.FirewallItem(clone)).ToBeVisibleAsync();
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
    public async Task User_Can_Delete_Firewall_From_Details_Page()
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
            await hardwareTree.GotoFirewallsListAsync();

            var list = new FirewallsListPom(page);
            await list.AssertLoadedAsync();

            await list.AddFirewallAsync(name);
            await list.OpenFirewallAsync(name);

            var card = new FirewallCardPom(page);

            await card.DeleteFirewallAsync(name);

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
    public async Task User_Can_Add_And_Remove_Tags_From_Firewall_Card()
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
            await hardwareTree.GotoFirewallsListAsync();

            var list = new FirewallsListPom(page);
            await list.AssertLoadedAsync();

            await list.AddFirewallAsync(name);
            await page.WaitForURLAsync($"**/resources/hardware/{name}");

            var card = new FirewallCardPom(page);

            var tags = card.Tags;

            // -------------------------------------------------
            // Add multiple tags in one modal interaction
            // -------------------------------------------------

            await tags.AddTagsAsync("firewall", "Foo", "Bar", "Baz");

            await tags.AssertTagVisibleAsync("firewall", "Foo");
            await tags.AssertTagVisibleAsync("firewall", "Bar");
            await tags.AssertTagVisibleAsync("firewall", "Baz");

            // -------------------------------------------------
            // Remove a single tag
            // -------------------------------------------------

            await tags.RemoveTagAsync("firewall", "Bar");

            await tags.AssertTagNotVisibleAsync("firewall", "Bar");
            await tags.AssertTagVisibleAsync("firewall", "Foo");
            await tags.AssertTagVisibleAsync("firewall", "Baz");

            // -------------------------------------------------
            // Reload to verify persistence
            // -------------------------------------------------

            await page.ReloadAsync();

            await tags.AssertTagVisibleAsync("firewall", "Foo");
            await tags.AssertTagVisibleAsync("firewall", "Baz");
            await tags.AssertTagNotVisibleAsync("firewall", "Bar");

            await context.CloseAsync();
        }
        finally
        {
            await context.CloseAsync();
        }
    }
}
