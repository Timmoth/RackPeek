namespace Tests.E2e.PageObjectModels;

using Microsoft.Playwright;

public class TagsPom(IPage page)
{
    // -------------------------------------------------
    // Root
    // -------------------------------------------------

    public ILocator Root(string testIdPrefix)
        => page.GetByTestId($"{testIdPrefix}-resource-tag-editor");

    public ILocator Header(string testIdPrefix)
        => Root(testIdPrefix).GetByTestId($"{testIdPrefix}-resource-tag-editor-header");

    public ILocator AddButton(string testIdPrefix)
        => Root(testIdPrefix).GetByTestId($"{testIdPrefix}-resource-tag-editor-add");

    public ILocator TagList(string testIdPrefix)
        => Root(testIdPrefix).GetByTestId($"{testIdPrefix}-resource-tag-editor-list");

    // -------------------------------------------------
    // Individual Tags
    // -------------------------------------------------

    public ILocator Tag(string testIdPrefix, string tag)
        => Root(testIdPrefix)
            .GetByTestId($"{testIdPrefix}-resource-tag-editor-tag-{tag}");

    public ILocator ViewTagButton(string testIdPrefix, string tag)
        => Root(testIdPrefix)
            .GetByTestId($"{testIdPrefix}-resource-tag-editor-tag-{tag}-view");

    public ILocator RemoveTagButton(string testIdPrefix, string tag)
        => Root(testIdPrefix)
            .GetByTestId($"{testIdPrefix}-resource-tag-editor-tag-{tag}-remove");

    // -------------------------------------------------
    // Modal (CommaSeparatedStringModal)
    // -------------------------------------------------

    public ILocator Modal(string testIdPrefix)
        => page.GetByTestId($"{testIdPrefix}-resource-tag-editor-comma-separated-string-modal");

    public ILocator ModalInput(string testIdPrefix)
        => page.GetByTestId($"{testIdPrefix}-resource-tag-editor-comma-separated-string-modal-input");

    public ILocator ModalSubmit(string testIdPrefix)
        => page.GetByTestId($"{testIdPrefix}-resource-tag-editor-comma-separated-string-modal-submit");

    public ILocator ModalCancel(string testIdPrefix)
        => page.GetByTestId($"{testIdPrefix}-resource-tag-editor-comma-separated-string-modal-cancel");

    // -------------------------------------------------
    // Assertions
    // -------------------------------------------------

    public async Task AssertTagVisibleAsync(string testIdPrefix, string tag)
    {
        await Assertions.Expect(Tag(testIdPrefix, tag)).ToBeVisibleAsync();
    }

    public async Task AssertTagNotVisibleAsync(string testIdPrefix, string tag)
    {
        await Assertions.Expect(Tag(testIdPrefix, tag)).Not.ToBeVisibleAsync();
    }

    // -------------------------------------------------
    // Actions
    // -------------------------------------------------

    public async Task AddTagsAsync(string testIdPrefix, params string[] tags)
    {
        await AddButton(testIdPrefix).ClickAsync();

        await Assertions.Expect(ModalInput(testIdPrefix)).ToBeVisibleAsync();

        var value = string.Join(", ", tags);

        await ModalInput(testIdPrefix).FillAsync(value);
        await ModalSubmit(testIdPrefix).ClickAsync();

        foreach (var tag in tags)
        {
            await AssertTagVisibleAsync(testIdPrefix, tag);
        }
    }

    public async Task RemoveTagAsync(string testIdPrefix, string tag)
    {
        await RemoveTagButton(testIdPrefix, tag).ClickAsync();
        await AssertTagNotVisibleAsync(testIdPrefix, tag);
    }

    public async Task NavigateToTagAsync(string testIdPrefix, string tag)
    {
        await ViewTagButton(testIdPrefix, tag).ClickAsync();
        await page.WaitForURLAsync($"**/tags/{tag}");
    }
}
