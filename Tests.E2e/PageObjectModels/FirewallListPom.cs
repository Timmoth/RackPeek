using Microsoft.Playwright;

namespace Tests.E2e.PageObjectModels;

public class FirewallsListPom(IPage page)
{
    public AddResourceComponent AddFirewall => new(page, "firewall");

    public ILocator PageRoot => page.GetByTestId("firewalls-page-root");
    public ILocator PageTitle => page.GetByTestId("firewalls-page-title");

    public ILocator Loading => page.GetByTestId("firewalls-loading");
    public ILocator EmptyState => page.GetByTestId("firewalls-empty");
    public ILocator FirewallsList => page.GetByTestId("firewalls-list");

    public ILocator AddSection => page.GetByTestId("firewalls-add-section");

    // Must match AddResourceComponent test IDs
    public ILocator AddInput => page.GetByTestId("add-firewall-input");
    public ILocator AddButton => page.GetByTestId("add-firewall-button");

    // -------------------------------------------------
    // Dynamic Firewall Items
    // -------------------------------------------------

    public ILocator FirewallItem(string name)
    {
        return page.GetByTestId($"firewall-item-{Sanitize(name)}");
    }

    public ILocator OpenLink(string name)
    {
        return FirewallItem(name)
            .GetByTestId("open-firewall-link");
    }

    public ILocator EditButton(string name)
    {
        return FirewallItem(name)
            .GetByTestId("edit-firewall-button");
    }

    public ILocator SaveButton(string name)
    {
        return FirewallItem(name)
            .GetByTestId("save-firewall-button");
    }

    public ILocator CancelButton(string name)
    {
        return FirewallItem(name)
            .GetByTestId("cancel-firewall-button");
    }

    public ILocator RenameButton(string name)
    {
        return FirewallItem(name)
            .GetByTestId("rename-firewall-button");
    }

    public ILocator CloneButton(string name)
    {
        return FirewallItem(name)
            .GetByTestId("clone-firewall-button");
    }

    public ILocator DeleteButton(string name)
    {
        return FirewallItem(name)
            .GetByTestId("delete-firewall-button");
    }

    // -------------------------------------------------
    // Navigation
    // -------------------------------------------------

    public async Task GotoAsync(string baseUrl)
    {
        await page.GotoAsync($"{baseUrl}/firewalls/list");
        await AssertLoadedAsync();
    }

    public async Task AssertLoadedAsync()
    {
        await Assertions.Expect(PageRoot).ToBeVisibleAsync();
        await Assertions.Expect(PageTitle).ToBeVisibleAsync();
    }

    public async Task WaitForListAsync()
    {
        await Assertions.Expect(FirewallsList).ToBeVisibleAsync();
    }

    // -------------------------------------------------
    // Actions
    // -------------------------------------------------

    public async Task AddFirewallAsync(string name)
    {
        await AddFirewall.AddAsync(name);
        await Assertions.Expect(FirewallItem(name))
            .ToBeVisibleAsync();
    }

    public async Task DeleteFirewallAsync(string name)
    {
        await DeleteButton(name).ClickAsync();
        await page.GetByTestId("Firewall-confirm-modal-confirm").ClickAsync();

        await Assertions.Expect(FirewallItem(name))
            .Not.ToBeVisibleAsync();
    }

    public async Task OpenFirewallAsync(string name)
    {
        await OpenLink(name).ClickAsync();
        await page.WaitForURLAsync($"**/resources/hardware/{name}");
    }

    public async Task AssertFirewallExists(string name)
    {
        await Assertions.Expect(FirewallItem(name))
            .ToBeVisibleAsync();
    }

    public async Task AssertFirewallDoesNotExist(string name)
    {
        await Assertions.Expect(FirewallItem(name))
            .Not.ToBeVisibleAsync();
    }

    private static string Sanitize(string value)
    {
        return value.Replace(" ", "-");
    }
}
