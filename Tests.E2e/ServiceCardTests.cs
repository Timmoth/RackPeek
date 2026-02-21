using Tests.E2e.Infra;
using Tests.E2e.PageObjectModels;
using Xunit.Abstractions;
using Microsoft.Playwright;

namespace Tests.E2e;

public class ServiceCardTests(
    PlaywrightFixture fixture,
    ITestOutputHelper output) : E2ETestBase(fixture, output)
{
    private readonly ITestOutputHelper _output = output;

    // =============================================================
    // Rename / Clone / Delete Flow
    // =============================================================

    [Fact]
    public async Task User_Can_Rename_Clone_And_Delete_Service_From_Details_Page()
    {
        var (context, page) = await CreatePageAsync();

        var originalName = $"e2e-svc-{Guid.NewGuid():N}"[..16];
        var renamedName  = $"e2e-svc-rn-{Guid.NewGuid():N}"[..16];
        var cloneName    = $"e2e-svc-cl-{Guid.NewGuid():N}"[..16];

        try
        {
            await page.GotoAsync($"{fixture.BaseUrl}/services/list");

            var list = new ServicesListPom(page);
            await list.AssertLoadedAsync();

            await list.AddServiceAsync(originalName);

            if (!page.Url.Contains($"/resources/services/{originalName}",
                    StringComparison.OrdinalIgnoreCase))
            {
                await list.OpenServiceAsync(originalName);
            }

            var card = new ServiceCardPom(page);
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
            await page.WaitForURLAsync("**/services/list");

            // Delete renamed original
            await page.GotoAsync($"{fixture.BaseUrl}/resources/services/{renamedName}");
            await card.AssertVisibleAsync(renamedName);

            await card.DeleteAsync(renamedName);
            await page.WaitForURLAsync("**/services/list");
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
    // Edit Flow Test
    // =============================================================

    [Fact]
    public async Task User_Can_Edit_And_Save_Service()
    {
        var (context, page) = await CreatePageAsync();
        var name = $"e2e-svc-edit-{Guid.NewGuid():N}"[..16];

        try
        {
            await page.GotoAsync($"{fixture.BaseUrl}/services/list");

            var list = new ServicesListPom(page);
            await list.AddServiceAsync(name);

            if (!page.Url.Contains($"/resources/services/{name}",
                    StringComparison.OrdinalIgnoreCase))
            {
                await list.OpenServiceAsync(name);
            }

            var card = new ServiceCardPom(page);
            await card.AssertVisibleAsync(name);

            await card.BeginEditAsync(name);

            // Fill via proper test ids
            await card.IpInput(name).FillAsync("127.0.0.1");
            await card.PortInput(name).FillAsync("8080");
            await card.ProtocolInput(name).FillAsync("http");
            await card.UrlInput(name).FillAsync("http://localhost:8080");

            await card.SaveAsync(name);

            // Verify edit mode exited
            await Assertions.Expect(card.EditButton(name)).ToBeVisibleAsync();

            // Verify persisted values
            await Assertions.Expect(card.IpValue(name)).ToHaveTextAsync("127.0.0.1");
            await Assertions.Expect(card.PortValue(name)).ToHaveTextAsync("8080");
            await Assertions.Expect(card.ProtocolValue(name)).ToHaveTextAsync("http");
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    // =============================================================
    // Cancel Edit Test
    // =============================================================

    [Fact]
    public async Task User_Can_Cancel_Edit_Without_Saving()
    {
        var (context, page) = await CreatePageAsync();
        var name = $"e2e-svc-cancel-{Guid.NewGuid():N}"[..16];

        try
        {
            await page.GotoAsync($"{fixture.BaseUrl}/services/list");

            var list = new ServicesListPom(page);
            await list.AddServiceAsync(name);

            if (!page.Url.Contains($"/resources/services/{name}",
                    StringComparison.OrdinalIgnoreCase))
            {
                await list.OpenServiceAsync(name);
            }

            var card = new ServiceCardPom(page);
            await card.AssertVisibleAsync(name);

            await card.BeginEditAsync(name);

            await card.IpInput(name).FillAsync("should-not-save");

            await card.CancelAsync(name);

            // Confirm edit mode exited
            await Assertions.Expect(card.EditButton(name)).ToBeVisibleAsync();

            // Confirm value did NOT persist
            await Assertions.Expect(card.IpValue(name)).Not.ToBeVisibleAsync();
        }
        finally
        {
            await context.CloseAsync();
        }
    }
    
    
    [Fact]
    public async Task User_Can_Add_And_Remove_Tags_From_Service_Card()
    {
        var (context, page) = await CreatePageAsync();
        var name = $"e2e-ap-{Guid.NewGuid():N}"[..16];

        try
        {
            await page.GotoAsync($"{fixture.BaseUrl}/services/list");

            var list = new ServicesListPom(page);
            await list.AssertLoadedAsync();

            await list.AddServiceAsync(name);
            await page.WaitForURLAsync($"**/resources/services/{name}");

            var card = new ServiceCardPom(page);
            await card.AssertVisibleAsync(name);

            var tags = card.Tags;

            // -------------------------------------------------
            // Add multiple tags in one modal interaction
            // -------------------------------------------------

            await tags.AddTagsAsync("service", "Foo", "Bar", "Baz");

            await tags.AssertTagVisibleAsync("service", "Foo");
            await tags.AssertTagVisibleAsync("service", "Bar");
            await tags.AssertTagVisibleAsync("service", "Baz");

            // -------------------------------------------------
            // Remove a single tag
            // -------------------------------------------------

            await tags.RemoveTagAsync("service", "Bar");

            await tags.AssertTagNotVisibleAsync("service", "Bar");
            await tags.AssertTagVisibleAsync("service", "Foo");
            await tags.AssertTagVisibleAsync("service", "Baz");

            // -------------------------------------------------
            // Reload to verify persistence
            // -------------------------------------------------

            await page.ReloadAsync();

            await tags.AssertTagVisibleAsync("service", "Foo");
            await tags.AssertTagVisibleAsync("service", "Baz");
            await tags.AssertTagNotVisibleAsync("service", "Bar");

            await context.CloseAsync();
        }
        finally
        {
            await context.CloseAsync();
        }
    }
}
