using Microsoft.Playwright;

namespace Tests.E2e.PageObjectModels;

public class ServicesListPom
{
    private readonly IPage _page;

    public ServicesListPom(IPage page)
    {
        _page = page;
    }

    // -------------------------------------------------
    // Root & State
    // -------------------------------------------------

    public ILocator PageRoot => _page.GetByTestId("services-page-root");
    public ILocator PageTitle => _page.GetByTestId("services-page-title");

    public ILocator Loading => _page.GetByTestId("services-loading");
    public ILocator EmptyState => _page.GetByTestId("services-empty");
    public ILocator ServicesList => _page.GetByTestId("services-list");

    // -------------------------------------------------
    // Add Service Component (Reusable Component Object)
    // -------------------------------------------------

    public AddResourceComponent AddService => new(_page, "service");

    // -------------------------------------------------
    // Grouping (RunsOn)
    // -------------------------------------------------

    public ILocator Group(string groupKey)
    {
        return _page.GetByTestId($"services-group-{SanitizeGroup(groupKey)}");
    }

    public ILocator GroupTitle(string groupKey)
    {
        return _page.GetByTestId($"services-group-title-{SanitizeGroup(groupKey)}");
    }

    public ILocator GroupList(string groupKey)
    {
        return _page.GetByTestId($"services-group-list-{SanitizeGroup(groupKey)}");
    }

    // -------------------------------------------------
    // Individual Services
    // -------------------------------------------------

    public ILocator ServiceListItem(string name)
    {
        return _page.GetByTestId($"services-list-item-{Sanitize(name)}");
    }

    public ILocator ServiceCard(string name)
    {
        return _page.GetByTestId($"service-item-{Sanitize(name)}");
    }

    public ILocator DeleteButton(string name)
    {
        return ServiceCard(name).GetByTestId("delete-service-button");
    }

    public ILocator EditButton(string name)
    {
        return ServiceCard(name).GetByTestId("edit-service-button");
    }

    public ILocator RenameButton(string name)
    {
        return ServiceCard(name).GetByTestId("rename-service-button");
    }

    public ILocator CloneButton(string name)
    {
        return ServiceCard(name).GetByTestId("clone-service-button");
    }

    // -------------------------------------------------
    // Navigation
    // -------------------------------------------------

    public async Task GotoAsync(string baseUrl)
    {
        await _page.GotoAsync($"{baseUrl}/services/list");
        await AssertLoadedAsync();
    }

    public async Task AssertLoadedAsync()
    {
        await Assertions.Expect(PageRoot).ToBeVisibleAsync();
        await Assertions.Expect(PageTitle).ToBeVisibleAsync();
    }

    public async Task WaitForListAsync()
    {
        await Assertions.Expect(ServicesList).ToBeVisibleAsync();
    }

    public async Task AssertEmptyAsync()
    {
        await Assertions.Expect(EmptyState).ToBeVisibleAsync();
    }

    // -------------------------------------------------
    // Actions
    // -------------------------------------------------

    public async Task AddServiceAsync(string name)
    {
        await AddService.AddAsync(name);

        await Assertions.Expect(ServiceCard(name))
            .ToBeVisibleAsync();
    }

    public async Task DeleteServiceAsync(string name)
    {
        await DeleteButton(name).ClickAsync();

        await _page.GetByTestId("Service-confirm-modal-confirm")
            .ClickAsync();

        await Assertions.Expect(ServiceCard(name))
            .Not.ToBeVisibleAsync();
    }

    public async Task OpenServiceAsync(string name)
    {
        await ServiceCard(name).ClickAsync();
        await _page.WaitForURLAsync($"**/resources/services/{name}");
    }

    // -------------------------------------------------
    // Assertions
    // -------------------------------------------------

    public async Task AssertServiceExists(string name)
    {
        await Assertions.Expect(ServiceCard(name))
            .ToBeVisibleAsync();
    }

    public async Task AssertServiceDoesNotExist(string name)
    {
        await Assertions.Expect(ServiceCard(name))
            .Not.ToBeVisibleAsync();
    }

    public async Task AssertGroupExists(string groupKey)
    {
        await Assertions.Expect(Group(groupKey))
            .ToBeVisibleAsync();
    }

    public async Task AssertServiceInGroup(string groupKey, string serviceName)
    {
        await Assertions.Expect(
                GroupList(groupKey)
                    .GetByTestId($"services-list-item-{Sanitize(serviceName)}")
            )
            .ToBeVisibleAsync();
    }

    // -------------------------------------------------
    // Utilities
    // -------------------------------------------------

    private static string Sanitize(string value)
    {
        return value.Replace(" ", "-");
    }

    private static string SanitizeGroup(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "unassigned"
            : value.Replace(" ", "-");
    }
}