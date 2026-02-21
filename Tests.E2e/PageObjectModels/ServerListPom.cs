using Microsoft.Playwright;

namespace Tests.E2e.PageObjectModels;

public class ServersListPom(IPage page)
{
    public AddResourceComponent AddServer => new(page, "server");

    public ILocator PageRoot => page.GetByTestId("servers-page-root");
    public ILocator PageTitle => page.GetByTestId("servers-page-title");

    public ILocator Loading => page.GetByTestId("servers-loading");
    public ILocator EmptyState => page.GetByTestId("servers-empty");
    public ILocator ServersList => page.GetByTestId("servers-list");

    public ILocator AddSection => page.GetByTestId("add-server-section");

    // These must match your AddResourceComponent test IDs
    public ILocator AddInput => page.GetByTestId("add-server-input");
    public ILocator AddButton => page.GetByTestId("add-server-button");

    // -------------------------------------------------
    // Dynamic Server Items
    // -------------------------------------------------

    public ILocator ServerItem(string serverName)
    {
        return page.GetByTestId($"server-item-{Sanitize(serverName)}");
    }

    public ILocator DeleteButton(string serverName)
    {
        return ServerItem(serverName)
            .GetByTestId("delete-server-button");
    }

    public ILocator EditButton(string serverName)
    {
        return ServerItem(serverName)
            .GetByTestId("edit-server-button");
    }

    public ILocator RenameButton(string serverName)
    {
        return ServerItem(serverName)
            .GetByTestId("rename-server-button");
    }

    public ILocator CloneButton(string serverName)
    {
        return ServerItem(serverName)
            .GetByTestId("clone-server-button");
    }

    // -------------------------------------------------
    // Navigation
    // -------------------------------------------------

    public async Task GotoAsync(string baseUrl)
    {
        await page.GotoAsync($"{baseUrl}/servers/list");
        await AssertLoadedAsync();
    }

    public async Task AssertLoadedAsync()
    {
        await Assertions.Expect(PageRoot).ToBeVisibleAsync();
        await Assertions.Expect(PageTitle).ToBeVisibleAsync();
    }

    public async Task WaitForListAsync()
    {
        await Assertions.Expect(ServersList).ToBeVisibleAsync();
    }

    // -------------------------------------------------
    // Actions
    // -------------------------------------------------

    public async Task AddServerAsync(string serverName)
    {
        await AddServer.AddAsync(serverName);
        await Assertions.Expect(ServerItem(serverName))
            .ToBeVisibleAsync();
    }

    public async Task DeleteServerAsync(string serverName)
    {
        await DeleteButton(serverName).ClickAsync();
        await page.GetByTestId("Server-confirm-modal-confirm").ClickAsync();

        await Assertions.Expect(ServerItem(serverName))
            .Not.ToBeVisibleAsync();
    }

    public async Task OpenServerAsync(string serverName)
    {
        await ServerItem(serverName).ClickAsync();
        await page.WaitForURLAsync($"**/resources/hardware/{serverName}");
    }

    public async Task AssertServerExists(string serverName)
    {
        await Assertions.Expect(ServerItem(serverName))
            .ToBeVisibleAsync();
    }

    public async Task AssertServerDoesNotExist(string serverName)
    {
        await Assertions.Expect(ServerItem(serverName))
            .Not.ToBeVisibleAsync();
    }

    private static string Sanitize(string value)
    {
        return value.Replace(" ", "-");
    }
}