using Microsoft.Playwright;

namespace Tests.E2e.PageObjectModels;

public class LabelsPom(IPage page) {
    public ILocator Root(string testIdPrefix)
        => page.GetByTestId($"{testIdPrefix}-resource-label-editor");

    public ILocator AddButton(string testIdPrefix)
        => Root(testIdPrefix).GetByTestId($"{testIdPrefix}-resource-label-editor-add");

    public ILocator Label(string testIdPrefix, string key)
        => Root(testIdPrefix).GetByTestId($"{testIdPrefix}-resource-label-editor-label-{key}");

    public ILocator LabelViewButton(string testIdPrefix, string key)
        => Root(testIdPrefix).GetByTestId($"{testIdPrefix}-resource-label-editor-label-{key}-view");

    public ILocator RemoveLabelButton(string testIdPrefix, string key)
        => Root(testIdPrefix).GetByTestId($"{testIdPrefix}-resource-label-editor-label-{key}-remove");

    public ILocator ModalKeyInput(string testIdPrefix)
        => page.GetByTestId($"{testIdPrefix}-resource-label-editor-key-value-modal-key-input");

    public ILocator ModalValueInput(string testIdPrefix)
        => page.GetByTestId($"{testIdPrefix}-resource-label-editor-key-value-modal-value-input");

    public ILocator ModalSubmit(string testIdPrefix)
        => page.GetByTestId($"{testIdPrefix}-resource-label-editor-key-value-modal-submit");

    public async Task AddLabelAsync(string testIdPrefix, string key, string value) {
        await AddButton(testIdPrefix).ClickAsync();
        await Assertions.Expect(ModalKeyInput(testIdPrefix)).ToBeVisibleAsync();
        await ModalKeyInput(testIdPrefix).FillAsync(key);
        await ModalValueInput(testIdPrefix).FillAsync(value);
        await ModalSubmit(testIdPrefix).ClickAsync();
    }

    public async Task EditLabelAsync(string testIdPrefix, string existingKey, string newKey, string newValue) {
        await LabelViewButton(testIdPrefix, existingKey).ClickAsync();
        await Assertions.Expect(ModalKeyInput(testIdPrefix)).ToBeVisibleAsync();
        await ModalKeyInput(testIdPrefix).FillAsync(newKey);
        await ModalValueInput(testIdPrefix).FillAsync(newValue);
        await ModalSubmit(testIdPrefix).ClickAsync();
    }

    public async Task RemoveLabelAsync(string testIdPrefix, string key) =>
        await RemoveLabelButton(testIdPrefix, key).ClickAsync();

    public async Task AssertLabelVisibleAsync(string testIdPrefix, string key) =>
        await Assertions.Expect(Label(testIdPrefix, key)).ToBeVisibleAsync();

    public async Task AssertLabelNotVisibleAsync(string testIdPrefix, string key) =>
        await Assertions.Expect(Label(testIdPrefix, key)).Not.ToBeVisibleAsync();

    public async Task AssertLabelDisplaysAsync(string testIdPrefix, string key, string expectedValue) {
        ILocator locator = Label(testIdPrefix, key);
        await Assertions.Expect(locator).ToBeVisibleAsync();
        await Assertions.Expect(locator).ToContainTextAsync($"{key}: {expectedValue}");
    }
}
