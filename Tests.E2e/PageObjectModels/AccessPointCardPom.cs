namespace Tests.E2e.PageObjectModels;

using Microsoft.Playwright;

public class AccessPointCardPom(IPage page)
{
    public TagsPom Tags => new(page);

    // Root
    public ILocator Card(string accessPointName)
        => page.GetByTestId($"accesspoint-item-{Sanitize(accessPointName)}");

    // Link / navigation
    public ILocator OpenLink(string accessPointName)
        => Card(accessPointName).GetByTestId("open-accesspoint-link");

    // Top-right actions (view mode)
    public ILocator EditButton(string accessPointName)
        => Card(accessPointName).GetByTestId("edit-accesspoint-button");

    public ILocator RenameButton(string accessPointName)
        => Card(accessPointName).GetByTestId("rename-accesspoint-button");

    public ILocator CloneButton(string accessPointName)
        => Card(accessPointName).GetByTestId("clone-accesspoint-button");

    public ILocator DeleteButton(string accessPointName)
        => Card(accessPointName).GetByTestId("delete-accesspoint-button");

    // Top-right actions (edit mode)
    public ILocator SaveButton(string accessPointName)
        => Card(accessPointName).GetByTestId("save-accesspoint-button");

    public ILocator CancelButton(string accessPointName)
        => Card(accessPointName).GetByTestId("cancel-accesspoint-button");

    // Fields
    public ILocator ModelSection(string accessPointName)
        => Card(accessPointName).GetByTestId("accesspoint-model-section");

    public ILocator ModelInput(string accessPointName)
        => Card(accessPointName).GetByTestId("accesspoint-model-input");

    public ILocator ModelValue(string accessPointName)
        => Card(accessPointName).GetByTestId("accesspoint-model-value");

    public ILocator SpeedSection(string accessPointName)
        => Card(accessPointName).GetByTestId("accesspoint-speed-section");

    public ILocator SpeedInput(string accessPointName)
        => Card(accessPointName).GetByTestId("accesspoint-speed-input");

    public ILocator SpeedValue(string accessPointName)
        => Card(accessPointName).GetByTestId("accesspoint-speed-value");

    // Notes (prefixed components)
    public ILocator NotesViewerRoot(string accessPointName)
        => Card(accessPointName).GetByTestId("accesspoint-notes-viewer-markdown-viewer");

    public ILocator NotesViewerContent(string accessPointName)
        => Card(accessPointName).GetByTestId("accesspoint-notes-viewer-markdown-viewer-content");

    public ILocator NotesViewerEmpty(string accessPointName)
        => Card(accessPointName).GetByTestId("accesspoint-notes-viewer-markdown-viewer-empty");

    public ILocator NotesEditorRoot(string accessPointName)
        => Card(accessPointName).GetByTestId("accesspoint-notes-editor-markdown-editor");

    public ILocator NotesEditorTextarea(string accessPointName)
        => Card(accessPointName).GetByTestId("accesspoint-notes-editor-markdown-editor-textarea");

    // Modals
    public ILocator DeleteConfirmModal => page.GetByTestId("AccessPoint-confirm-modal");
    public ILocator DeleteConfirmButton => page.GetByTestId("AccessPoint-confirm-modal-confirm");
    public ILocator DeleteCancelButton => page.GetByTestId("AccessPoint-confirm-modal-cancel");

    public ILocator RenameModal => page.GetByTestId("accesspoint-rename-string-value-modal");
    public ILocator RenameModalInput => page.GetByTestId("accesspoint-rename-string-value-modal-input");
    public ILocator RenameModalSubmit => page.GetByTestId("accesspoint-rename-string-value-modal-submit");
    public ILocator RenameModalCancel => page.GetByTestId("accesspoint-rename-string-value-modal-cancel");

    public ILocator CloneModal => page.GetByTestId("accesspoint-clone-string-value-modal");
    public ILocator CloneModalInput => page.GetByTestId("accesspoint-clone-string-value-modal-input");
    public ILocator CloneModalSubmit => page.GetByTestId("accesspoint-clone-string-value-modal-submit");
    public ILocator CloneModalCancel => page.GetByTestId("accesspoint-clone-string-value-modal-cancel");

    // -------------------------------------------------
    // Navigation (hardware details page)
    // -------------------------------------------------

    public async Task GotoHardwareAsync(string baseUrl, string hardwareName)
    {
        await page.GotoAsync($"{baseUrl}/resources/hardware/{hardwareName}");
        await AssertCardVisibleAsync(hardwareName);
    }

    public async Task AssertCardVisibleAsync(string accessPointName)
    {
        await Assertions.Expect(Card(accessPointName)).ToBeVisibleAsync();
    }

    // -------------------------------------------------
    // Actions
    // -------------------------------------------------

    public async Task OpenAsync(string accessPointName)
    {
        await OpenLink(accessPointName).ClickAsync();
        await page.WaitForURLAsync($"**/resources/hardware/{accessPointName}");
    }

    public async Task BeginEditAsync(string accessPointName)
    {
        await EditButton(accessPointName).ClickAsync();
        await Assertions.Expect(ModelInput(accessPointName)).ToBeVisibleAsync();
    }

    public async Task SetModelAsync(string accessPointName, string model)
    {
        await ModelInput(accessPointName).FillAsync(model);
    }

    public async Task SetSpeedAsync(string accessPointName, double speed)
    {
        await SpeedInput(accessPointName).FillAsync(speed.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public async Task SaveAsync(string accessPointName)
    {
        await SaveButton(accessPointName).ClickAsync();
        await Assertions.Expect(ModelSection(accessPointName)).ToBeVisibleAsync();
    }

    public async Task CancelEditAsync(string accessPointName)
    {
        await CancelButton(accessPointName).ClickAsync();
        await Assertions.Expect(EditButton(accessPointName)).ToBeVisibleAsync();
    }

    public async Task DeleteAsync(string accessPointName)
    {
        await DeleteButton(accessPointName).ClickAsync();
        await DeleteConfirmButton.ClickAsync();

        await Assertions.Expect(Card(accessPointName))
            .Not.ToBeVisibleAsync();
    }

    public async Task RenameAsync(string accessPointName, string newName)
    {
        await RenameButton(accessPointName).ClickAsync();
        await RenameModalInput.FillAsync(newName);
        await RenameModalSubmit.ClickAsync();

        await page.WaitForURLAsync($"**/resources/hardware/{newName}");
    }

    public async Task CloneAsync(string accessPointName, string cloneName)
    {
        await CloneButton(accessPointName).ClickAsync();
        await CloneModalInput.FillAsync(cloneName);
        await CloneModalSubmit.ClickAsync();

        await page.WaitForURLAsync($"**/resources/hardware/{cloneName}");
    }

    private static string Sanitize(string value)
        => value.Replace(" ", "-");
}
