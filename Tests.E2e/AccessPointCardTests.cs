using System.Globalization;
using Microsoft.Playwright;
using Tests.E2e.Infra;
using Tests.E2e.PageObjectModels;
using Xunit.Abstractions;

namespace Tests.E2e;

public class AccessPointCardTests(
    PlaywrightFixture fixture,
    ITestOutputHelper output) : E2ETestBase(fixture, output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public async Task User_Can_Edit_Model_And_Speed_And_Save()
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
            await hardwareTree.GotoAccessPointsListAsync();

            var list = new AccessPointsListPom(page);
            await list.AssertLoadedAsync();

            await list.AddAccessPointAsync(name);
            await page.WaitForURLAsync($"**/resources/hardware/{name}");

            var card = new AccessPointCardPom(page);
            await card.AssertCardVisibleAsync(name);

            var newModel = "AP-Model-9000";
            var newSpeed = 2.5;

            await card.BeginEditAsync(name);
            await card.SetModelAsync(name, newModel);
            await card.SetSpeedAsync(name, newSpeed);
            await card.SaveAsync(name);

            await page.ReloadAsync();
            
            await Assertions.Expect(card.ModelValue(name)).ToHaveTextAsync(newModel);
            await Assertions.Expect(card.SpeedValue(name))
                .ToHaveTextAsync($"{newSpeed.ToString(CultureInfo.InvariantCulture)} Gbps");

            await context.CloseAsync();
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task User_Can_Cancel_Edit_And_Changes_Are_Not_Applied()
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
            await hardwareTree.GotoAccessPointsListAsync();

            var list = new AccessPointsListPom(page);
            await list.AssertLoadedAsync();

            await list.AddAccessPointAsync(name);
            await page.WaitForURLAsync($"**/resources/hardware/{name}");

            var card = new AccessPointCardPom(page);
            await card.AssertCardVisibleAsync(name);

            // Capture current values (may be empty depending on seed data)
            var beforeModel = await card.ModelSection(name).TextContentAsync();
            var beforeSpeed = await card.SpeedSection(name).TextContentAsync();

            await card.BeginEditAsync(name);
            await card.SetModelAsync(name, "SHOULD-NOT-SAVE");
            await card.SetSpeedAsync(name, 9.9);
            await card.CancelEditAsync(name);

            var afterModel = await card.ModelSection(name).TextContentAsync();
            var afterSpeed = await card.SpeedSection(name).TextContentAsync();

            Assert.Equal(beforeModel, afterModel);
            Assert.Equal(beforeSpeed, afterSpeed);

            await context.CloseAsync();
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task User_Can_Rename_AccessPoint_From_Card()
    {
        var (context, page) = await CreatePageAsync();
        var name = $"e2e-ap-{Guid.NewGuid():N}"[..16];
        var newName = $"e2e-ap-{Guid.NewGuid():N}"[..16];

        try
        {
            await page.GotoAsync(fixture.BaseUrl);

            var layout = new MainLayoutPom(page);
            await layout.AssertLoadedAsync();
            await layout.GotoHardwareAsync();

            var hardwareTree = new HardwareTreePom(page);
            await hardwareTree.AssertLoadedAsync();
            await hardwareTree.GotoAccessPointsListAsync();

            var list = new AccessPointsListPom(page);
            await list.AssertLoadedAsync();

            await list.AddAccessPointAsync(name);
            await page.WaitForURLAsync($"**/resources/hardware/{name}");

            var card = new AccessPointCardPom(page);
            await card.AssertCardVisibleAsync(name);

            await card.RenameAsync(name, newName);

            // After rename, the card test id uses the new name
            await card.AssertCardVisibleAsync(newName);

            await context.CloseAsync();
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task User_Can_Clone_AccessPoint_From_Card()
    {
        var (context, page) = await CreatePageAsync();
        var name = $"e2e-ap-{Guid.NewGuid():N}"[..16];
        var cloneName = $"e2e-ap-{Guid.NewGuid():N}"[..16];

        try
        {
            await page.GotoAsync(fixture.BaseUrl);

            var layout = new MainLayoutPom(page);
            await layout.AssertLoadedAsync();
            await layout.GotoHardwareAsync();

            var hardwareTree = new HardwareTreePom(page);
            await hardwareTree.AssertLoadedAsync();
            await hardwareTree.GotoAccessPointsListAsync();

            var list = new AccessPointsListPom(page);
            await list.AssertLoadedAsync();

            await list.AddAccessPointAsync(name);
            await page.WaitForURLAsync($"**/resources/hardware/{name}");

            var card = new AccessPointCardPom(page);
            await card.AssertCardVisibleAsync(name);

            await card.CloneAsync(name, cloneName);

            // Clone navigates to the clone details page
            await card.AssertCardVisibleAsync(cloneName);

            // Cleanup: delete clone then original (both from details pages)
            await card.DeleteAsync(cloneName);
            await page.WaitForURLAsync("**/hardware/tree");

            await hardwareTree.GotoAccessPointsListAsync();
            await list.AssertLoadedAsync();
            await list.OpenAccessPointAsync(name);

            await card.AssertCardVisibleAsync(name);
            await card.DeleteAsync(name);

            await context.CloseAsync();
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task User_Can_Delete_AccessPoint_From_Card_And_Is_Redirected()
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
            await hardwareTree.GotoAccessPointsListAsync();

            var list = new AccessPointsListPom(page);
            await list.AssertLoadedAsync();

            await list.AddAccessPointAsync(name);
            await page.WaitForURLAsync($"**/resources/hardware/{name}");

            var card = new AccessPointCardPom(page);
            await card.AssertCardVisibleAsync(name);

            await card.DeleteAsync(name);
            await page.WaitForURLAsync("**/hardware/tree");

            // Verify it’s gone from the list
            await hardwareTree.GotoAccessPointsListAsync();
            await list.AssertLoadedAsync();
            await list.AssertAccessPointDoesNotExist(name);

            await context.CloseAsync();
        }
        finally
        {
            await context.CloseAsync();
        }
    }
    
    [Fact]
    public async Task User_Can_Add_And_Remove_Tags_From_AccessPoint_Card()
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
            await hardwareTree.GotoAccessPointsListAsync();

            var list = new AccessPointsListPom(page);
            await list.AssertLoadedAsync();

            await list.AddAccessPointAsync(name);
            await page.WaitForURLAsync($"**/resources/hardware/{name}");

            var card = new AccessPointCardPom(page);
            await card.AssertCardVisibleAsync(name);

            var tags = card.Tags;

            // -------------------------------------------------
            // Add multiple tags in one modal interaction
            // -------------------------------------------------

            await tags.AddTagsAsync("accesspoint", "Foo", "Bar", "Baz");

            await tags.AssertTagVisibleAsync("accesspoint", "Foo");
            await tags.AssertTagVisibleAsync("accesspoint", "Bar");
            await tags.AssertTagVisibleAsync("accesspoint", "Baz");

            // -------------------------------------------------
            // Remove a single tag
            // -------------------------------------------------

            await tags.RemoveTagAsync("accesspoint", "Bar");

            await tags.AssertTagNotVisibleAsync("accesspoint", "Bar");
            await tags.AssertTagVisibleAsync("accesspoint", "Foo");
            await tags.AssertTagVisibleAsync("accesspoint", "Baz");

            // -------------------------------------------------
            // Reload to verify persistence
            // -------------------------------------------------

            await page.ReloadAsync();

            await tags.AssertTagVisibleAsync("accesspoint", "Foo");
            await tags.AssertTagVisibleAsync("accesspoint", "Baz");
            await tags.AssertTagNotVisibleAsync("accesspoint", "Bar");

            await context.CloseAsync();
        }
        finally
        {
            await context.CloseAsync();
        }
    }
}
