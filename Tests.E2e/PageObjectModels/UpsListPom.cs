using Microsoft.Playwright;

namespace Tests.E2e.PageObjectModels;

public class UpsListPom(IPage page)
{
    public AddResourceComponent AddUps => new(page, "ups");

    public ILocator PageRoot => page.GetByTestId("ups-page-root");
    public ILocator PageTitle => page.GetByTestId("ups-page-title");

    public ILocator Loading => page.GetByTestId("ups-loading");
    public ILocator EmptyState => page.GetByTestId("ups-empty");
    public ILocator UpsList => page.GetByTestId("ups-list");

    public ILocator AddSection => page.GetByTestId("ups-add-section");

    // Must match AddResourceComponent test IDs
    public ILocator AddInput => page.GetByTestId("add-ups-input");
    public ILocator AddButton => page.GetByTestId("add-ups-button");

    // -------------------------------------------------
    // Dynamic Ups Items
    // -------------------------------------------------

    public ILocator UpsItem(string name)
    {
        return page.GetByTestId($"ups-item-{Sanitize(name)}");
    }

    public ILocator OpenLink(string name)
    {
        return UpsItem(name)
            .GetByTestId("open-ups-link");
    }

    public ILocator EditButton(string name)
    {
        return UpsItem(name)
            .GetByTestId("edit-ups-button");
    }

    public ILocator SaveButton(string name)
    {
        return UpsItem(name)
            .GetByTestId("save-ups-button");
    }

    public ILocator CancelButton(string name)
    {
        return UpsItem(name)
            .GetByTestId("cancel-ups-button");
    }

    public ILocator RenameButton(string name)
    {
        return UpsItem(name)
            .GetByTestId("rename-ups-button");
    }

    public ILocator CloneButton(string name)
    {
        return UpsItem(name)
            .GetByTestId("clone-ups-button");
    }

    public ILocator DeleteButton(string name)
    {
        return UpsItem(name)
            .GetByTestId("delete-ups-button");
    }

    // -------------------------------------------------
    // Navigation
    // -------------------------------------------------

    public async Task GotoAsync(string baseUrl)
    {
        await page.GotoAsync($"{baseUrl}/ups/list");
        await AssertLoadedAsync();
    }

    public async Task AssertLoadedAsync()
    {
        await Assertions.Expect(PageRoot).ToBeVisibleAsync();
        await Assertions.Expect(PageTitle).ToBeVisibleAsync();
    }

    public async Task WaitForListAsync()
    {
        await Assertions.Expect(UpsList).ToBeVisibleAsync();
    }

    // -------------------------------------------------
    // Actions
    // -------------------------------------------------

    public async Task AddUpsAsync(string name)
    {
        await AddUps.AddAsync(name);
        await Assertions.Expect(UpsItem(name))
            .ToBeVisibleAsync();
    }

    public async Task DeleteUpsAsync(string name)
    {
        await DeleteButton(name).ClickAsync();
        await page.GetByTestId("Ups-confirm-modal-confirm").ClickAsync();

        await Assertions.Expect(UpsItem(name))
            .Not.ToBeVisibleAsync();
    }

    public async Task OpenUpsAsync(string name)
    {
        await OpenLink(name).ClickAsync();
        await page.WaitForURLAsync($"**/resources/hardware/{name}");
    }

    public async Task AssertUpsExists(string name)
    {
        await Assertions.Expect(UpsItem(name))
            .ToBeVisibleAsync();
    }

    public async Task AssertUpsDoesNotExist(string name)
    {
        await Assertions.Expect(UpsItem(name))
            .Not.ToBeVisibleAsync();
    }

    private static string Sanitize(string value)
    {
        return value.Replace(" ", "-");
    }
}
