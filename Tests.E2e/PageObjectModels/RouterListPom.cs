using Microsoft.Playwright;

namespace Tests.E2e.PageObjectModels;

public class RouterListPom(IPage page)
{
    public AddResourceComponent AddRouter => new(page, "router");

    public ILocator PageRoot => page.GetByTestId("routers-page-root");
    public ILocator PageTitle => page.GetByTestId("routers-page-title");

    public ILocator Loading => page.GetByTestId("routers-loading");
    public ILocator EmptyState => page.GetByTestId("routers-empty");
    public ILocator RoutersList => page.GetByTestId("routers-list");

    public ILocator AddSection => page.GetByTestId("routers-add-section");

    // Must match AddResourceComponent test IDs
    public ILocator AddInput => page.GetByTestId("add-router-input");
    public ILocator AddButton => page.GetByTestId("add-router-button");

    // -------------------------------------------------
    // Dynamic Router Items
    // -------------------------------------------------

    public ILocator RouterItem(string name)
    {
        return page.GetByTestId($"router-item-{Sanitize(name)}");
    }

    public ILocator OpenLink(string name)
    {
        return RouterItem(name)
            .GetByTestId("open-router-link");
    }

    public ILocator EditButton(string name)
    {
        return RouterItem(name)
            .GetByTestId("edit-router-button");
    }

    public ILocator SaveButton(string name)
    {
        return RouterItem(name)
            .GetByTestId("save-router-button");
    }

    public ILocator CancelButton(string name)
    {
        return RouterItem(name)
            .GetByTestId("cancel-router-button");
    }

    public ILocator RenameButton(string name)
    {
        return RouterItem(name)
            .GetByTestId("rename-router-button");
    }

    public ILocator CloneButton(string name)
    {
        return RouterItem(name)
            .GetByTestId("clone-router-button");
    }

    public ILocator DeleteButton(string name)
    {
        return RouterItem(name)
            .GetByTestId("delete-router-button");
    }

    // -------------------------------------------------
    // Navigation
    // -------------------------------------------------

    public async Task GotoAsync(string baseUrl)
    {
        await page.GotoAsync($"{baseUrl}/routers/list");
        await AssertLoadedAsync();
    }

    public async Task AssertLoadedAsync()
    {
        await Assertions.Expect(PageRoot).ToBeVisibleAsync();
        await Assertions.Expect(PageTitle).ToBeVisibleAsync();
    }

    public async Task WaitForListAsync()
    {
        await Assertions.Expect(RoutersList).ToBeVisibleAsync();
    }

    // -------------------------------------------------
    // Actions
    // -------------------------------------------------

    public async Task AddRouterAsync(string name)
    {
        await AddRouter.AddAsync(name);
        await Assertions.Expect(RouterItem(name))
            .ToBeVisibleAsync();
    }

    public async Task DeleteRouterAsync(string name)
    {
        await DeleteButton(name).ClickAsync();
        await page.GetByTestId("Router-confirm-modal-confirm").ClickAsync();

        await Assertions.Expect(RouterItem(name))
            .Not.ToBeVisibleAsync();
    }

    public async Task OpenRouterAsync(string name)
    {
        await OpenLink(name).ClickAsync();
        await page.WaitForURLAsync($"**/resources/hardware/{name}");
    }

    public async Task AssertRouterExists(string name)
    {
        await Assertions.Expect(RouterItem(name))
            .ToBeVisibleAsync();
    }

    public async Task AssertRouterDoesNotExist(string name)
    {
        await Assertions.Expect(RouterItem(name))
            .Not.ToBeVisibleAsync();
    }

    private static string Sanitize(string value)
    {
        return value.Replace(" ", "-");
    }
}
