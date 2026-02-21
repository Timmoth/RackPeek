using Microsoft.Playwright;

namespace Tests.E2e.PageObjectModels;

public class SwitchListPom(IPage page)
{
    public AddResourceComponent AddSwitch => new(page, "switch");

    public ILocator PageRoot => page.GetByTestId("switches-page-root");
    public ILocator PageTitle => page.GetByTestId("switches-page-title");

    public ILocator Loading => page.GetByTestId("switches-loading");
    public ILocator EmptyState => page.GetByTestId("switches-empty");
    public ILocator SwitchsList => page.GetByTestId("switches-list");

    public ILocator AddSection => page.GetByTestId("switches-add-section");

    // Must match AddResourceComponent test IDs
    public ILocator AddInput => page.GetByTestId("add-switch-input");
    public ILocator AddButton => page.GetByTestId("add-switch-button");

    // -------------------------------------------------
    // Dynamic Switch Items
    // -------------------------------------------------

    public ILocator SwitchItem(string name)
    {
        return page.GetByTestId($"switch-item-{Sanitize(name)}");
    }

    public ILocator OpenLink(string name)
    {
        return SwitchItem(name)
            .GetByTestId("open-switch-link");
    }

    public ILocator EditButton(string name)
    {
        return SwitchItem(name)
            .GetByTestId("edit-switch-button");
    }

    public ILocator SaveButton(string name)
    {
        return SwitchItem(name)
            .GetByTestId("save-switch-button");
    }

    public ILocator CancelButton(string name)
    {
        return SwitchItem(name)
            .GetByTestId("cancel-switch-button");
    }

    public ILocator RenameButton(string name)
    {
        return SwitchItem(name)
            .GetByTestId("rename-switch-button");
    }

    public ILocator CloneButton(string name)
    {
        return SwitchItem(name)
            .GetByTestId("clone-switch-button");
    }

    public ILocator DeleteButton(string name)
    {
        return SwitchItem(name)
            .GetByTestId("delete-switch-button");
    }

    // -------------------------------------------------
    // Navigation
    // -------------------------------------------------

    public async Task GotoAsync(string baseUrl)
    {
        await page.GotoAsync($"{baseUrl}/switches/list");
        await AssertLoadedAsync();
    }

    public async Task AssertLoadedAsync()
    {
        await Assertions.Expect(PageRoot).ToBeVisibleAsync();
        await Assertions.Expect(PageTitle).ToBeVisibleAsync();
    }

    public async Task WaitForListAsync()
    {
        await Assertions.Expect(SwitchsList).ToBeVisibleAsync();
    }

    // -------------------------------------------------
    // Actions
    // -------------------------------------------------

    public async Task AddSwitchAsync(string name)
    {
        await AddSwitch.AddAsync(name);
        await Assertions.Expect(SwitchItem(name))
            .ToBeVisibleAsync();
    }

    public async Task DeleteSwitchAsync(string name)
    {
        await DeleteButton(name).ClickAsync();
        await page.GetByTestId("Switch-confirm-modal-confirm").ClickAsync();

        await Assertions.Expect(SwitchItem(name))
            .Not.ToBeVisibleAsync();
    }

    public async Task OpenSwitchAsync(string name)
    {
        await OpenLink(name).ClickAsync();
        await page.WaitForURLAsync($"**/resources/hardware/{name}");
    }

    public async Task AssertSwitchExists(string name)
    {
        await Assertions.Expect(SwitchItem(name))
            .ToBeVisibleAsync();
    }

    public async Task AssertSwitchDoesNotExist(string name)
    {
        await Assertions.Expect(SwitchItem(name))
            .Not.ToBeVisibleAsync();
    }

    private static string Sanitize(string value)
    {
        return value.Replace(" ", "-");
    }
}
