using Microsoft.Playwright;

namespace Tests.E2e.PageObjectModels;

public class AddResourceComponent(IPage page, string resourceType)
{
    private readonly string _resourceType = resourceType.ToLower();

    // -------------------------------------------------
    // Root & Structure
    // -------------------------------------------------

    public ILocator Root
        => page.GetByTestId($"add-{_resourceType}-root");

    public ILocator Title
        => page.GetByTestId($"add-{_resourceType}-title");

    public ILocator Form
        => page.GetByTestId($"add-{_resourceType}-form");

    public ILocator Input
        => page.GetByTestId($"add-{_resourceType}-input");

    public ILocator Button
        => page.GetByTestId($"add-{_resourceType}-button");

    public ILocator Error
        => page.GetByTestId($"add-{_resourceType}-error");

    // -------------------------------------------------
    // Actions
    // -------------------------------------------------

    public async Task AddAsync(string name)
    {
        await Assertions.Expect(Root).ToBeVisibleAsync();

        await Input.FillAsync(name);
        await Button.ClickAsync();
    }

    public async Task AssertErrorAsync(string message)
    {
        await Assertions.Expect(Error)
            .ToHaveTextAsync(message);
    }

    public async Task AssertVisibleAsync()
    {
        await Assertions.Expect(Root).ToBeVisibleAsync();
    }
}