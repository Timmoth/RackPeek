using Microsoft.Playwright;

namespace Tests.E2e.PageObjectModels;

public class SystemsListPom
{
    private readonly IPage _page;

    public SystemsListPom(IPage page)
    {
        _page = page;
    }

    public AddResourceComponent AddSystem => new(_page, "system");

    // -------------------------------------------------
    // Root & State
    // -------------------------------------------------

    public ILocator PageRoot => _page.GetByTestId("systems-page-root");
    public ILocator PageTitle => _page.GetByTestId("systems-page-title");

    public ILocator Loading => _page.GetByTestId("systems-loading");
    public ILocator EmptyState => _page.GetByTestId("systems-empty");
    public ILocator SystemsList => _page.GetByTestId("systems-list");

    // -------------------------------------------------
    // Add System Section
    // -------------------------------------------------

    public ILocator AddSection => _page.GetByTestId("add-system-section");

    // These must match AddResourceComponent test IDs
    public ILocator AddInput => _page.GetByTestId("add-system-input");
    public ILocator AddButton => _page.GetByTestId("add-system-button");

    // -------------------------------------------------
    // Grouping (RunsOn)
    // -------------------------------------------------

    public ILocator Group(string groupKey)
    {
        return _page.GetByTestId($"systems-group-{SanitizeGroup(groupKey)}");
    }

    public ILocator GroupTitle(string groupKey)
    {
        return _page.GetByTestId($"systems-group-title-{SanitizeGroup(groupKey)}");
    }

    public ILocator GroupList(string groupKey)
    {
        return _page.GetByTestId($"systems-group-list-{SanitizeGroup(groupKey)}");
    }

    // -------------------------------------------------
    // Individual Systems
    // -------------------------------------------------

    public ILocator SystemListItem(string name)
    {
        return _page.GetByTestId($"systems-list-item-{Sanitize(name)}");
    }

    public ILocator SystemCard(string name)
    {
        return _page.GetByTestId($"system-item-{Sanitize(name)}");
    }

    public ILocator DeleteButton(string name)
    {
        return SystemCard(name).GetByTestId("delete-system-button");
    }

    public ILocator EditButton(string name)
    {
        return SystemCard(name).GetByTestId("edit-system-button");
    }

    public ILocator RenameButton(string name)
    {
        return SystemCard(name).GetByTestId("rename-system-button");
    }

    public ILocator CloneButton(string name)
    {
        return SystemCard(name).GetByTestId("clone-system-button");
    }

    // -------------------------------------------------
    // Navigation
    // -------------------------------------------------

    public async Task GotoAsync(string baseUrl)
    {
        await _page.GotoAsync($"{baseUrl}/systems/list");
        await AssertLoadedAsync();
    }

    public async Task AssertLoadedAsync()
    {
        await Assertions.Expect(PageRoot).ToBeVisibleAsync();
        await Assertions.Expect(PageTitle).ToBeVisibleAsync();
    }

    public async Task WaitForListAsync()
    {
        await Assertions.Expect(SystemsList).ToBeVisibleAsync();
    }

    public async Task AssertEmptyAsync()
    {
        await Assertions.Expect(EmptyState).ToBeVisibleAsync();
    }

    // -------------------------------------------------
    // Actions
    // -------------------------------------------------

    public async Task AddSystemAsync(string name)
    {
        await AddSystem.AddAsync(name);

        await Assertions.Expect(SystemCard(name))
            .ToBeVisibleAsync();
    }

    public async Task DeleteSystemAsync(string name)
    {
        await DeleteButton(name).ClickAsync();

        await _page.GetByTestId("System-confirm-modal-confirm")
            .ClickAsync();

        await Assertions.Expect(SystemCard(name))
            .Not.ToBeVisibleAsync();
    }

    public async Task OpenSystemAsync(string name)
    {
        await SystemCard(name).ClickAsync();
        await _page.WaitForURLAsync($"**/resources/systems/{name}");
    }

    // -------------------------------------------------
    // Assertions
    // -------------------------------------------------

    public async Task AssertSystemExists(string name)
    {
        await Assertions.Expect(SystemCard(name))
            .ToBeVisibleAsync();
    }

    public async Task AssertSystemDoesNotExist(string name)
    {
        await Assertions.Expect(SystemCard(name))
            .Not.ToBeVisibleAsync();
    }

    public async Task AssertGroupExists(string groupKey)
    {
        await Assertions.Expect(Group(groupKey))
            .ToBeVisibleAsync();
    }

    public async Task AssertSystemInGroup(string groupKey, string systemName)
    {
        await Assertions.Expect(
                GroupList(groupKey)
                    .GetByTestId($"systems-list-item-{Sanitize(systemName)}")
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