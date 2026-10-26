using Microsoft.Playwright;
using Tests.E2e.Infra;
using Tests.E2e.PageObjectModels;
using Xunit.Abstractions;

namespace Tests.E2e;

public class OtherCardTests(
    PlaywrightFixture fixture,
    ITestOutputHelper output) : E2ETestBase(fixture, output) {
    private readonly PlaywrightFixture _fixture = fixture;
    private readonly ITestOutputHelper _output = output;

    // =============================================================
    // Rename + Clone + Delete Flow
    // =============================================================

    [Fact]
    public async Task User_Can_Rename_Clone_And_Delete_Other_From_Details_Page() {
        (IBrowserContext context, IPage page) = await CreatePageAsync();

        var originalName = $"e2e-oth-{Guid.NewGuid():N}"[..16];
        var renamedName = $"e2e-oth-rn-{Guid.NewGuid():N}"[..16];
        var cloneName = $"e2e-oth-cl-{Guid.NewGuid():N}"[..16];

        try {
            await page.GotoAsync($"{_fixture.BaseUrl}/other/list");

            var list = new OtherListPom(page);
            await list.AddOtherAsync(originalName);

            if (!page.Url.Contains($"/resources/hardware/{originalName}",
                    StringComparison.OrdinalIgnoreCase))
                await list.OpenOtherAsync(originalName);

            var card = new OtherCardPom(page);
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
            await page.GotoAsync($"{_fixture.BaseUrl}/resources/hardware/{renamedName}");
            await card.AssertVisibleAsync(renamedName);

            await card.DeleteAsync(renamedName);
            await page.WaitForURLAsync("**/hardware/tree");
        }
        catch (Exception) {
            _output.WriteLine("TEST FAILED — Capturing diagnostics");
            _output.WriteLine($"Current URL: {page.Url}");

            var html = await page.ContentAsync();
            _output.WriteLine("==== DOM SNAPSHOT START ====");
            _output.WriteLine(html);
            _output.WriteLine("==== DOM SNAPSHOT END ====");

            throw;
        }
        finally {
            await context.CloseAsync();
        }
    }

    // =============================================================
    // Edit + Save Flow
    // =============================================================

    [Fact]
    public async Task User_Can_Edit_And_Save_Other() {
        (IBrowserContext context, IPage page) = await CreatePageAsync();
        var name = $"e2e-oth-edit-{Guid.NewGuid():N}"[..16];

        try {
            await page.GotoAsync($"{_fixture.BaseUrl}/other/list");

            var list = new OtherListPom(page);
            await list.AddOtherAsync(name);

            if (!page.Url.Contains($"/resources/hardware/{name}",
                    StringComparison.OrdinalIgnoreCase))
                await list.OpenOtherAsync(name);

            var card = new OtherCardPom(page);
            await card.AssertVisibleAsync(name);

            await card.BeginEditAsync(name);

            await card.ModelInput(name).FillAsync("Building Bridge XG");
            await card.DescriptionInput(name).FillAsync("Microwave radio bridge");

            await card.SaveAsync(name);

            await Assertions.Expect(
                card.ModelValue(name)
            ).ToContainTextAsync("Building Bridge XG");

            await Assertions.Expect(
                card.DescriptionValue(name)
            ).ToContainTextAsync("Microwave radio bridge");
        }
        finally {
            await context.CloseAsync();
        }
    }

    // =============================================================
    // Cancel Edit Flow
    // =============================================================

    [Fact]
    public async Task User_Can_Cancel_Other_Edit_Without_Saving() {
        (IBrowserContext context, IPage page) = await CreatePageAsync();
        var name = $"e2e-oth-cancel-{Guid.NewGuid():N}"[..16];

        try {
            await page.GotoAsync($"{_fixture.BaseUrl}/other/list");

            var list = new OtherListPom(page);
            await list.AddOtherAsync(name);

            if (!page.Url.Contains($"/resources/hardware/{name}",
                    StringComparison.OrdinalIgnoreCase))
                await list.OpenOtherAsync(name);

            var card = new OtherCardPom(page);
            await card.AssertVisibleAsync(name);

            await card.BeginEditAsync(name);

            await card.ModelInput(name).FillAsync("ShouldNotPersist");
            await card.DescriptionInput(name).FillAsync("ShouldNotPersist");

            await card.CancelAsync(name);

            // Verify edit mode exited
            await Assertions.Expect(
                card.EditButton(name)
            ).ToBeVisibleAsync();
        }
        finally {
            await context.CloseAsync();
        }
    }


    [Fact]
    public async Task User_Can_Add_And_Remove_Tags_From_Other_Card() {
        (IBrowserContext context, IPage page) = await CreatePageAsync();
        var name = $"e2e-oth-{Guid.NewGuid():N}"[..16];

        try {
            await page.GotoAsync(_fixture.BaseUrl);

            var layout = new MainLayoutPom(page);
            await layout.AssertLoadedAsync();
            await layout.GotoHardwareAsync();

            var hardwareTree = new HardwareTreePom(page);
            await hardwareTree.AssertLoadedAsync();
            await hardwareTree.GotoOtherListAsync();

            var list = new OtherListPom(page);
            await list.AssertLoadedAsync();

            await list.AddOtherAsync(name);
            await page.WaitForURLAsync($"**/resources/hardware/{name}");

            var card = new OtherCardPom(page);
            await card.AssertVisibleAsync(name);

            TagsPom tags = card.Tags;

            // -------------------------------------------------
            // Add multiple tags in one modal interaction
            // -------------------------------------------------

            await tags.AddTagsAsync("other", "Foo", "Bar", "Baz");

            await tags.AssertTagVisibleAsync("other", "Foo");
            await tags.AssertTagVisibleAsync("other", "Bar");
            await tags.AssertTagVisibleAsync("other", "Baz");

            // -------------------------------------------------
            // Remove a single tag
            // -------------------------------------------------

            await tags.RemoveTagAsync("other", "Bar");

            await tags.AssertTagNotVisibleAsync("other", "Bar");
            await tags.AssertTagVisibleAsync("other", "Foo");
            await tags.AssertTagVisibleAsync("other", "Baz");

            // -------------------------------------------------
            // Reload to verify persistence
            // -------------------------------------------------

            await page.ReloadAsync();

            await tags.AssertTagVisibleAsync("other", "Foo");
            await tags.AssertTagVisibleAsync("other", "Baz");
            await tags.AssertTagNotVisibleAsync("other", "Bar");

            await context.CloseAsync();
        }
        finally {
            await context.CloseAsync();
        }
    }

    // =============================================================
    // Ports
    // =============================================================

    [Fact]
    public async Task User_Can_Add_Port_Groups_To_Other_Hardware() {
        (IBrowserContext context, IPage page) = await CreatePageAsync();

        var name = $"e2e-oth-{Guid.NewGuid():N}"[..16];

        try {
            await page.GotoAsync($"{_fixture.BaseUrl}/other/list");

            var list = new OtherListPom(page);
            await list.AddOtherAsync(name);

            if (!page.Url.Contains($"/resources/hardware/{name}",
                    StringComparison.OrdinalIgnoreCase))
                await list.OpenOtherAsync(name);

            var card = new OtherCardPom(page);
            await card.AssertVisibleAsync(name);

            await Assertions.Expect(card.PortGroupSection).ToBeVisibleAsync();

            await card.AddPortGroupAsync("rj45", "0.1", 1);
            await card.AssertPortGroupVisibleAsync(0);

            await card.AddPortGroupAsync("usb", "0.48", 2);
            await card.AssertPortGroupVisibleAsync(1);

            await page.ReloadAsync();
            await card.AssertVisibleAsync(name);

            await card.AssertPortVisibleAsync(0, 0);
            await card.AssertPortVisibleAsync(1, 0);
            await card.AssertPortVisibleAsync(1, 1);

            await card.DeleteAsync(name);
        }
        finally {
            await context.CloseAsync();
        }
    }
}
