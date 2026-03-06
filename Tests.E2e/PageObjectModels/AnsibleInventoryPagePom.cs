using Microsoft.Playwright;

namespace Tests.E2e.PageObjectModels;

public class AnsibleInventoryPagePom(IPage page) {
    // -------------------------------------------------
    // Root
    // -------------------------------------------------

    public ILocator Page
        => page.GetByTestId("ansible-inventory-page");

    // -------------------------------------------------
    // Actions
    // -------------------------------------------------

    public ILocator GenerateButton
        => page.GetByTestId("generate-inventory-button");

    // -------------------------------------------------
    // Inputs
    // -------------------------------------------------

    public ILocator GroupByTagsInput
        => page.GetByTestId("group-by-tags-input");

    public ILocator GroupByLabelsInput
        => page.GetByTestId("group-by-labels-input");

    public ILocator GlobalVarsInput
        => page.GetByTestId("global-vars-input");

    // -------------------------------------------------
    // Output
    // -------------------------------------------------

    public ILocator InventoryOutput
        => page.GetByTestId("inventory-output");

    public ILocator WarningsContainer
        => page.GetByTestId("inventory-warnings");

    // -------------------------------------------------
    // High-Level Actions
    // -------------------------------------------------

    public async Task AssertVisibleAsync()
        => await Assertions.Expect(Page).ToBeVisibleAsync();

    public async Task SetGroupByTagsAsync(string value)
        => await GroupByTagsInput.FillAsync(value);

    public async Task SetGroupByLabelsAsync(string value)
        => await GroupByLabelsInput.FillAsync(value);

    public async Task SetGlobalVarsAsync(string value)
        => await GlobalVarsInput.FillAsync(value);

    public async Task GenerateAsync()
        => await GenerateButton.ClickAsync();

    public async Task<string> GetInventoryTextAsync()
        => await InventoryOutput.InputValueAsync();

    public async Task AssertInventoryContainsAsync(string text)
        => await Assertions.Expect(InventoryOutput).ToContainTextAsync(text);

    public async Task AssertNoWarningsAsync()
        => await Assertions.Expect(WarningsContainer).ToHaveCountAsync(0);

    public async Task AssertWarningContainsAsync(string text)
        => await Assertions.Expect(WarningsContainer).ToContainTextAsync(text);
}
