using Microsoft.Playwright;

namespace Tests.E2e.PageObjectModels;

public class AccessPointsListPom(IPage page) {
    public AddResourceComponent AddAccessPoint => new(page, "accesspoint");

    public ILocator PageRoot => page.GetByTestId("accesspoints-page-root");
    public ILocator PageTitle => page.GetByTestId("accesspoints-page-title");

    public ILocator Loading => page.GetByTestId("accesspoints-loading");
    public ILocator EmptyState => page.GetByTestId("accesspoints-empty");
    public ILocator AccessPointsList => page.GetByTestId("accesspoints-list");

    public ILocator AddSection => page.GetByTestId("accesspoints-add-section");

    public ILocator AddInput => page.GetByTestId("add-accesspoint-input");
    public ILocator AddButton => page.GetByTestId("add-accesspoint-button");

    // -------------------------------------------------
    // Dynamic AccessPoint Items
    // -------------------------------------------------

    public ILocator AccessPointItem(string name) => page.GetByTestId($"accesspoint-item-{Sanitize(name)}");

    public ILocator AccessPointItemLink(string name) => page.GetByTestId($"open-accesspoint-{Sanitize(name)}-link");

    public ILocator DeleteButton(string name) {
        return AccessPointItem(name)
            .GetByTestId("delete-accesspoint-button");
    }

    public ILocator EditButton(string name) {
        return AccessPointItem(name)
            .GetByTestId("edit-accesspoint-button");
    }

    public ILocator RenameButton(string name) {
        return AccessPointItem(name)
            .GetByTestId("rename-accesspoint-button");
    }

    public ILocator CloneButton(string name) {
        return AccessPointItem(name)
            .GetByTestId("clone-accesspoint-button");
    }

    // -------------------------------------------------
    // Navigation
    // -------------------------------------------------

    public async Task GotoAsync(string baseUrl) {
        await page.GotoAsync($"{baseUrl}/accesspoints/list");
        await AssertLoadedAsync();
    }

    public async Task AssertLoadedAsync() {
        await Assertions.Expect(PageRoot).ToBeVisibleAsync();
        await Assertions.Expect(PageTitle).ToBeVisibleAsync();
    }

    public async Task WaitForListAsync() => await Assertions.Expect(AccessPointsList).ToBeVisibleAsync();

    // -------------------------------------------------
    // Actions
    // -------------------------------------------------

    public async Task AddAccessPointAsync(string name) {
        await AddAccessPoint.AddAsync(name);
        await Assertions.Expect(AccessPointItem(name))
            .ToBeVisibleAsync();
    }

    public async Task DeleteAccessPointAsync(string name) {
        await DeleteButton(name).ClickAsync();
        await page.GetByTestId("AccessPoint-confirm-modal-confirm").ClickAsync();

        await Assertions.Expect(AccessPointItem(name))
            .Not.ToBeVisibleAsync();
    }

    public async Task OpenAccessPointAsync(string name) {
        await AccessPointItemLink(name).ClickAsync();
        await page.WaitForURLAsync($"**/resources/hardware/{name}");
    }

    public async Task AssertAccessPointExists(string name) {
        await Assertions.Expect(AccessPointItem(name))
            .ToBeVisibleAsync();
    }

    public async Task AssertAccessPointDoesNotExist(string name) {
        await Assertions.Expect(AccessPointItem(name))
            .Not.ToBeVisibleAsync();
    }

    private static string Sanitize(string value) => value.Replace(" ", "-");
}
