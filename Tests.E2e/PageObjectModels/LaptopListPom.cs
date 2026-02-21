using Microsoft.Playwright;

namespace Tests.E2e.PageObjectModels;

public class LaptopListPom(IPage page)
{
    public AddResourceComponent Addlaptop => new(page, "laptop");

    public ILocator PageRoot => page.GetByTestId("laptops-page-root");
    public ILocator PageTitle => page.GetByTestId("laptops-page-title");

    public ILocator Loading => page.GetByTestId("laptops-loading");
    public ILocator EmptyState => page.GetByTestId("laptops-empty");
    public ILocator LaptopsList => page.GetByTestId("laptops-list");

    public ILocator AddSection => page.GetByTestId("laptops-add-section");

    // Must match AddResourceComponent test IDs
    public ILocator AddInput => page.GetByTestId("add-laptop-input");
    public ILocator AddButton => page.GetByTestId("add-laptop-button");

    // -------------------------------------------------
    // Dynamic laptop Items
    // -------------------------------------------------

    public ILocator LaptopItem(string name)
    {
        return page.GetByTestId($"laptop-item-{Sanitize(name)}");
    }

    public ILocator OpenLink(string name)
    {
        return LaptopItem(name)
            .GetByTestId("open-laptop-link");
    }

    public ILocator DeleteButton(string name)
    {
        return LaptopItem(name)
            .GetByTestId("delete-laptop-button");
    }

    public ILocator RenameButton(string name)
    {
        return LaptopItem(name)
            .GetByTestId("rename-laptop-button");
    }

    public ILocator CloneButton(string name)
    {
        return LaptopItem(name)
            .GetByTestId("clone-laptop-button");
    }

    public ILocator ModelBadge(string name)
    {
        return LaptopItem(name)
            .GetByTestId("laptop-model-badge");
    }

    // -------------------------------------------------
    // Navigation
    // -------------------------------------------------

    public async Task GotoAsync(string baseUrl)
    {
        await page.GotoAsync($"{baseUrl}/laptops/list");
        await AssertLoadedAsync();
    }

    public async Task AssertLoadedAsync()
    {
        await Assertions.Expect(PageRoot).ToBeVisibleAsync();
        await Assertions.Expect(PageTitle).ToBeVisibleAsync();
    }

    public async Task WaitForListAsync()
    {
        await Assertions.Expect(LaptopsList).ToBeVisibleAsync();
    }

    // -------------------------------------------------
    // Actions
    // -------------------------------------------------

    public async Task AddLaptopAsync(string name)
    {
        await Addlaptop.AddAsync(name);
        await Assertions.Expect(LaptopItem(name))
            .ToBeVisibleAsync();
    }

    public async Task DeleteLaptopAsync(string name)
    {
        await DeleteButton(name).ClickAsync();
        await page.GetByTestId("Laptop-confirm-modal-confirm").ClickAsync();

        await Assertions.Expect(LaptopItem(name))
            .Not.ToBeVisibleAsync();
    }

    public async Task OpenLaptopAsync(string name)
    {
        await OpenLink(name).ClickAsync();
        await page.WaitForURLAsync($"**/resources/hardware/{name}");
    }

    public async Task AssertLaptopExists(string name)
    {
        await Assertions.Expect(LaptopItem(name))
            .ToBeVisibleAsync();
    }

    public async Task AssertLaptopDoesNotExist(string name)
    {
        await Assertions.Expect(LaptopItem(name))
            .Not.ToBeVisibleAsync();
    }

    private static string Sanitize(string value)
    {
        return value.Replace(" ", "-");
    }
}
