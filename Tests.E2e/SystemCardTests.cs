using Tests.E2e.Infra;
using Tests.E2e.PageObjectModels;
using Xunit.Abstractions;
using Microsoft.Playwright;

namespace Tests.E2e;

public class SystemCardTests(
    PlaywrightFixture fixture,
    ITestOutputHelper output) : E2ETestBase(fixture, output)
{
    private readonly ITestOutputHelper _output = output;

    // ============================================================
    // Rename / Clone / Delete Flow
    // ============================================================

    [Fact]
    public async Task User_Can_Rename_Clone_And_Delete_System()
    {
        var (context, page) = await CreatePageAsync();

        var originalName = $"e2e-sys-{Guid.NewGuid():N}"[..16];
        var renamedName  = $"e2e-sys-rn-{Guid.NewGuid():N}"[..16];
        var cloneName    = $"e2e-sys-cl-{Guid.NewGuid():N}"[..16];

        try
        {
            await page.GotoAsync($"{fixture.BaseUrl}/systems/list");

            var list = new SystemsListPom(page);
            await list.AssertLoadedAsync();

            await list.AddSystemAsync(originalName);

            if (!page.Url.Contains($"/resources/systems/{originalName}",
                    StringComparison.OrdinalIgnoreCase))
            {
                await list.OpenSystemAsync(originalName);
            }

            var card = new SystemCardPom(page);
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
            await page.WaitForURLAsync("**/systems/list");

            // Delete renamed original
            await page.GotoAsync($"{fixture.BaseUrl}/resources/systems/{renamedName}");
            await card.AssertVisibleAsync(renamedName);

            await card.DeleteAsync(renamedName);
            await page.WaitForURLAsync("**/systems/list");
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

    // ============================================================
    // Edit Flow
    // ============================================================

    [Fact]
    public async Task User_Can_Edit_And_Save_System()
    {
        var (context, page) = await CreatePageAsync();
        var name = $"e2e-sys-edit-{Guid.NewGuid():N}"[..16];

        try
        {
            await page.GotoAsync($"{fixture.BaseUrl}/systems/list");

            var list = new SystemsListPom(page);
            await list.AddSystemAsync(name);

            if (!page.Url.Contains($"/resources/systems/{name}",
                    StringComparison.OrdinalIgnoreCase))
            {
                await list.OpenSystemAsync(name);
            }

            var card = new SystemCardPom(page);
            await card.AssertVisibleAsync(name);

            await card.BeginEditAsync(name);

            // Edit fields
            await card.TypeSelect(name).SelectOptionAsync(new SelectOptionValue { Index = 0 });
            await card.OsInput(name).FillAsync("Ubuntu 22.04");
            await card.CoresInput(name).FillAsync("8");
            await card.RamInput(name).FillAsync("16");

            await card.SaveAsync(name);

            // Verify read mode restored
            await Assertions.Expect(card.EditButton(name)).ToBeVisibleAsync();
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    // ============================================================
    // Cancel Edit
    // ============================================================

    [Fact]
    public async Task User_Can_Cancel_System_Edit()
    {
        var (context, page) = await CreatePageAsync();
        var name = $"e2e-sys-cancel-{Guid.NewGuid():N}"[..16];

        try
        {
            await page.GotoAsync($"{fixture.BaseUrl}/systems/list");

            var list = new SystemsListPom(page);
            await list.AddSystemAsync(name);

            if (!page.Url.Contains($"/resources/systems/{name}",
                    StringComparison.OrdinalIgnoreCase))
            {
                await list.OpenSystemAsync(name);
            }

            var card = new SystemCardPom(page);
            await card.AssertVisibleAsync(name);

            await card.BeginEditAsync(name);

            await card.OsInput(name).FillAsync("Should Not Save");

            await card.CancelAsync(name);

            await Assertions.Expect(card.EditButton(name)).ToBeVisibleAsync();
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    // ============================================================
    // Drive Flow
    // ============================================================

    [Fact]
    public async Task User_Can_Add_And_Edit_System_Drive()
    {
        var (context, page) = await CreatePageAsync();
        var name = $"e2e-sys-drive-{Guid.NewGuid():N}"[..16];

        try
        {
            await page.GotoAsync($"{fixture.BaseUrl}/systems/list");

            var list = new SystemsListPom(page);
            await list.AddSystemAsync(name);

            if (!page.Url.Contains($"/resources/systems/{name}",
                    StringComparison.OrdinalIgnoreCase))
            {
                await list.OpenSystemAsync(name);
            }

            var card = new SystemCardPom(page);
            await card.AssertVisibleAsync(name);

            await card.AddDriveAsync(name, "ssd", 512);
        }
        finally
        {
            await context.CloseAsync();
        }
    }
    
    
    [Fact]
    public async Task User_Can_Add_And_Remove_Tags_From_System_Card()
    {
        var (context, page) = await CreatePageAsync();
        var name = $"e2e-ap-{Guid.NewGuid():N}"[..16];

        try
        {
            await page.GotoAsync($"{fixture.BaseUrl}/systems/list");

            var list = new SystemsListPom(page);
            await list.AssertLoadedAsync();

            await list.AddSystemAsync(name);
            await page.WaitForURLAsync($"**/resources/systems/{name}");

            var card = new SystemCardPom(page);
            await card.AssertVisibleAsync(name);

            var tags = card.Tags;

            // -------------------------------------------------
            // Add multiple tags in one modal interaction
            // -------------------------------------------------

            await tags.AddTagsAsync("system", "Foo", "Bar", "Baz");

            await tags.AssertTagVisibleAsync("system", "Foo");
            await tags.AssertTagVisibleAsync("system", "Bar");
            await tags.AssertTagVisibleAsync("system", "Baz");

            // -------------------------------------------------
            // Remove a single tag
            // -------------------------------------------------

            await tags.RemoveTagAsync("system", "Bar");

            await tags.AssertTagNotVisibleAsync("system", "Bar");
            await tags.AssertTagVisibleAsync("system", "Foo");
            await tags.AssertTagVisibleAsync("system", "Baz");

            // -------------------------------------------------
            // Reload to verify persistence
            // -------------------------------------------------

            await page.ReloadAsync();

            await tags.AssertTagVisibleAsync("system", "Foo");
            await tags.AssertTagVisibleAsync("system", "Baz");
            await tags.AssertTagNotVisibleAsync("system", "Bar");

            await context.CloseAsync();
        }
        finally
        {
            await context.CloseAsync();
        }
    }

}
