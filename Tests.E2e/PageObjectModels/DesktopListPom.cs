using Microsoft.Playwright;

namespace Tests.E2e.PageObjectModels;

public class DesktopsListPom(IPage page)
{
    public AddResourceComponent AddDesktop => new(page, "desktop");

    public ILocator PageRoot => page.GetByTestId("desktops-page-root");
    public ILocator PageTitle => page.GetByTestId("desktops-page-title");

    public ILocator Loading => page.GetByTestId("desktops-loading");
    public ILocator EmptyState => page.GetByTestId("desktops-empty");
    public ILocator DesktopsList => page.GetByTestId("desktops-list");

    public ILocator AddSection => page.GetByTestId("desktops-add-section");

    // Must match AddResourceComponent test IDs
    public ILocator AddInput => page.GetByTestId("add-desktop-input");
    public ILocator AddButton => page.GetByTestId("add-desktop-button");

    // -------------------------------------------------
    // Dynamic Desktop Items
    // -------------------------------------------------

    public ILocator DesktopItem(string name)
    {
        return page.GetByTestId($"desktop-item-{Sanitize(name)}");
    }

    public ILocator OpenLink(string name)
    {
        return DesktopItem(name)
            .GetByTestId("open-desktop-link");
    }

    public ILocator DeleteButton(string name)
    {
        return DesktopItem(name)
            .GetByTestId("delete-desktop-button");
    }

    public ILocator RenameButton(string name)
    {
        return DesktopItem(name)
            .GetByTestId("rename-desktop-button");
    }

    public ILocator CloneButton(string name)
    {
        return DesktopItem(name)
            .GetByTestId("clone-desktop-button");
    }

    public ILocator ModelBadge(string name)
    {
        return DesktopItem(name)
            .GetByTestId("desktop-model-badge");
    }

    // -------------------------------------------------
    // Navigation
    // -------------------------------------------------

    public async Task GotoAsync(string baseUrl)
    {
        await page.GotoAsync($"{baseUrl}/desktops/list");
        await AssertLoadedAsync();
    }

    public async Task AssertLoadedAsync()
    {
        await Assertions.Expect(PageRoot).ToBeVisibleAsync();
        await Assertions.Expect(PageTitle).ToBeVisibleAsync();
    }

    public async Task WaitForListAsync()
    {
        await Assertions.Expect(DesktopsList).ToBeVisibleAsync();
    }

    // -------------------------------------------------
    // Actions
    // -------------------------------------------------

    public async Task AddDesktopAsync(string name)
    {
        await AddDesktop.AddAsync(name);
        await Assertions.Expect(DesktopItem(name))
            .ToBeVisibleAsync();
    }

    public async Task DeleteDesktopAsync(string name)
    {
        await DeleteButton(name).ClickAsync();
        await page.GetByTestId("Desktop-confirm-modal-confirm").ClickAsync();

        await Assertions.Expect(DesktopItem(name))
            .Not.ToBeVisibleAsync();
    }

    public async Task OpenDesktopAsync(string name)
    {
        await OpenLink(name).ClickAsync();
        await page.WaitForURLAsync($"**/resources/hardware/{name}");
    }

    public async Task AssertDesktopExists(string name)
    {
        await Assertions.Expect(DesktopItem(name))
            .ToBeVisibleAsync();
    }

    public async Task AssertDesktopDoesNotExist(string name)
    {
        await Assertions.Expect(DesktopItem(name))
            .Not.ToBeVisibleAsync();
    }

    private static string Sanitize(string value)
    {
        return value.Replace(" ", "-");
    }
}
