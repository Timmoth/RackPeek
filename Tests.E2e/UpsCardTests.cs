using Microsoft.Playwright;
using Tests.E2e.Infra;
using Tests.E2e.PageObjectModels;
using Xunit.Abstractions;

namespace Tests.E2e;

public class UpsCardTests(
    PlaywrightFixture fixture,
    ITestOutputHelper output) : E2ETestBase(fixture, output)
{
    private readonly ITestOutputHelper _output = output;

    // =============================================================
    // Rename + Clone + Delete Flow
    // =============================================================

    [Fact]
    public async Task User_Can_Rename_Clone_And_Delete_Ups_From_Details_Page()
    {
        var (context, page) = await CreatePageAsync();

        var originalName = $"e2e-ups-{Guid.NewGuid():N}"[..16];
        var renamedName  = $"e2e-ups-rn-{Guid.NewGuid():N}"[..16];
        var cloneName    = $"e2e-ups-cl-{Guid.NewGuid():N}"[..16];

        try
        {
            await page.GotoAsync($"{fixture.BaseUrl}/ups/list");

            var list = new UpsListPom(page);
            await list.AddUpsAsync(originalName);

            if (!page.Url.Contains($"/resources/hardware/{originalName}",
                    StringComparison.OrdinalIgnoreCase))
            {
                await list.OpenUpsAsync(originalName);
            }

            var card = new UpsCardPom(page);
            await card.AssertVisibleAsync(originalName);

            // -------------------------
            // Rename
            // -------------------------
            await card.RenameAsync(originalName, renamedName);
            await card.AssertVisibleAsync(renamedName);

            // -------------------------
            // Clone
            // -------------------------
            await card.CloneAsync(renamedName, cloneName);
            await card.AssertVisibleAsync(cloneName);

            // -------------------------
            // Delete clone
            // -------------------------
            await card.DeleteAsync(cloneName);
            await page.WaitForURLAsync("**/hardware/tree");

            // Navigate back and delete renamed
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

    // =============================================================
    // Edit + Save Flow
    // =============================================================

    [Fact]
    public async Task User_Can_Edit_And_Save_Ups()
    {
        var (context, page) = await CreatePageAsync();
        var name = $"e2e-ups-edit-{Guid.NewGuid():N}"[..16];

        try
        {
            await page.GotoAsync($"{fixture.BaseUrl}/ups/list");

            var list = new UpsListPom(page);
            await list.AddUpsAsync(name);

            if (!page.Url.Contains($"/resources/hardware/{name}",
                    StringComparison.OrdinalIgnoreCase))
            {
                await list.OpenUpsAsync(name);
            }

            var card = new UpsCardPom(page);
            await card.AssertVisibleAsync(name);

            await card.BeginEditAsync(name);

            await card.ModelInput(name).FillAsync("APC Smart-UPS");
            await card.CapacityInput(name).FillAsync("1500");

            await card.SaveAsync(name);

            await Assertions.Expect(
                card.ModelValue(name)
            ).ToContainTextAsync("APC Smart-UPS");

            await Assertions.Expect(
                card.CapacityValue(name)
            ).ToContainTextAsync("1500");
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    // =============================================================
    // Cancel Edit Flow
    // =============================================================

    [Fact]
    public async Task User_Can_Cancel_Ups_Edit_Without_Saving()
    {
        var (context, page) = await CreatePageAsync();
        var name = $"e2e-ups-cancel-{Guid.NewGuid():N}"[..16];

        try
        {
            await page.GotoAsync($"{fixture.BaseUrl}/ups/list");

            var list = new UpsListPom(page);
            await list.AddUpsAsync(name);

            if (!page.Url.Contains($"/resources/hardware/{name}",
                    StringComparison.OrdinalIgnoreCase))
            {
                await list.OpenUpsAsync(name);
            }

            var card = new UpsCardPom(page);
            await card.AssertVisibleAsync(name);

            await card.BeginEditAsync(name);

            await card.ModelInput(name).FillAsync("ShouldNotPersist");
            await card.CapacityInput(name).FillAsync("9999");

            await card.CancelAsync(name);

            // Verify edit mode exited
            await Assertions.Expect(
                card.EditButton(name)
            ).ToBeVisibleAsync();
        }
        finally
        {
            await context.CloseAsync();
        }
    }
    
    
    [Fact]
    public async Task User_Can_Add_And_Remove_Tags_From_Ups_Card()
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
            await hardwareTree.GotoUpsListAsync();

            var list = new UpsListPom(page);
            await list.AssertLoadedAsync();

            await list.AddUpsAsync(name);
            await page.WaitForURLAsync($"**/resources/hardware/{name}");

            var card = new UpsCardPom(page);
            await card.AssertVisibleAsync(name);

            var tags = card.Tags;

            // -------------------------------------------------
            // Add multiple tags in one modal interaction
            // -------------------------------------------------

            await tags.AddTagsAsync("ups", "Foo", "Bar", "Baz");

            await tags.AssertTagVisibleAsync("ups", "Foo");
            await tags.AssertTagVisibleAsync("ups", "Bar");
            await tags.AssertTagVisibleAsync("ups", "Baz");

            // -------------------------------------------------
            // Remove a single tag
            // -------------------------------------------------

            await tags.RemoveTagAsync("ups", "Bar");

            await tags.AssertTagNotVisibleAsync("ups", "Bar");
            await tags.AssertTagVisibleAsync("ups", "Foo");
            await tags.AssertTagVisibleAsync("ups", "Baz");

            // -------------------------------------------------
            // Reload to verify persistence
            // -------------------------------------------------

            await page.ReloadAsync();

            await tags.AssertTagVisibleAsync("ups", "Foo");
            await tags.AssertTagVisibleAsync("ups", "Baz");
            await tags.AssertTagNotVisibleAsync("ups", "Bar");

            await context.CloseAsync();
        }
        finally
        {
            await context.CloseAsync();
        }
    }
}
