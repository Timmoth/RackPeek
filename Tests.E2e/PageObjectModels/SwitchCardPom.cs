namespace Tests.E2e.PageObjectModels;

using Microsoft.Playwright;

public class SwitchCardPom(IPage page)
{
    public TagsPom Tags => new(page);

    // -------------------------------------------------
    // Dynamic Switch Item (root)
    // -------------------------------------------------

    public ILocator SwitchItem(string name)
        => page.GetByTestId($"switch-item-{Sanitize(name)}");

    public ILocator OpenLink(string name)
        => page.GetByTestId($"switch-item-{Sanitize(name)}-link");
    
    // -------------------------------------------------
    // Header Actions
    // -------------------------------------------------

    public ILocator EditButton(string name)
        => SwitchItem(name).GetByTestId("edit-switch-button");

    public ILocator SaveButton(string name)
        => SwitchItem(name).GetByTestId("save-switch-button");

    public ILocator CancelButton(string name)
        => SwitchItem(name).GetByTestId("cancel-switch-button");

    public ILocator RenameButton(string name)
        => SwitchItem(name).GetByTestId("rename-switch-button");

    public ILocator CloneButton(string name)
        => SwitchItem(name).GetByTestId("clone-switch-button");

    public ILocator DeleteButton(string name)
        => SwitchItem(name).GetByTestId("delete-switch-button");

    // -------------------------------------------------
    // Model
    // -------------------------------------------------

    public ILocator ModelSection(string name)
        => SwitchItem(name).GetByTestId("switch-model-section");

    public ILocator ModelInput(string name)
        => SwitchItem(name).GetByTestId("switch-model-input");

    public ILocator ModelValue(string name)
        => SwitchItem(name).GetByTestId("switch-model-value");

    // -------------------------------------------------
    // Features
    // -------------------------------------------------

    public ILocator FeaturesSection(string name)
        => SwitchItem(name).GetByTestId("switch-features-section");

    public ILocator ManagedCheckbox(string name)
        => SwitchItem(name).GetByTestId("switch-managed-checkbox");

    public ILocator PoeCheckbox(string name)
        => SwitchItem(name).GetByTestId("switch-poe-checkbox");

    public ILocator ManagedBadge(string name)
        => SwitchItem(name).GetByTestId("switch-managed-badge");

    public ILocator PoeBadge(string name)
        => SwitchItem(name).GetByTestId("switch-poe-badge");

    // -------------------------------------------------
    // Ports
    // -------------------------------------------------

    public ILocator PortsSection(string name)
        => SwitchItem(name).GetByTestId("switch-ports-section");

    public ILocator AddPortButton(string name)
        => SwitchItem(name).GetByTestId("add-port-button");

    public ILocator EditPortButton(string switchName, string portType, double portSpeed)
        => SwitchItem(switchName).GetByTestId($"edit-port-{portType}-{portSpeed}");

    // -------------------------------------------------
    // Notes (Markdown)
    // -------------------------------------------------

    public ILocator NotesSection(string name)
        => SwitchItem(name).GetByTestId("switch-notes-section");

    // MarkdownViewer / MarkdownEditor now use TestIdPrefix internally
    public ILocator NotesViewerRoot(string name)
        => SwitchItem(name).GetByTestId("switch-notes-viewer");

    public ILocator NotesViewerContent(string name)
        => SwitchItem(name).GetByTestId("switch-notes-viewer-content");

    public ILocator NotesEditorRoot(string name)
        => SwitchItem(name).GetByTestId("switch-notes-editor");

    public ILocator NotesEditorTextarea(string name)
        => SwitchItem(name).GetByTestId("switch-notes-editor-textarea");

    public ILocator NotesEditorSave(string name)
        => SwitchItem(name).GetByTestId("switch-notes-editor-save");

    public ILocator NotesEditorCancel(string name)
        => SwitchItem(name).GetByTestId("switch-notes-editor-cancel");

    // -------------------------------------------------
    // Modals
    // -------------------------------------------------

    public ILocator DeleteConfirmConfirmButton()
        => page.GetByTestId("Switch-confirm-modal-confirm");

    public ILocator DeleteConfirmCancelButton()
        => page.GetByTestId("Switch-confirm-modal-cancel");

    // StringValueModal (rename)
    public ILocator RenameModal()
        => page.GetByTestId("switch-rename-string-value-modal");

    public ILocator RenameInput()
        => page.GetByTestId("switch-rename-string-value-modal-input");

    public ILocator RenameAccept()
        => page.GetByTestId("switch-rename-string-value-modal-submit");

    public ILocator RenameCancel()
        => page.GetByTestId("switch-rename-string-value-modal-cancel");

    // StringValueModal (clone)
    public ILocator CloneModal()
        => page.GetByTestId("switch-clone-string-value-modal");

    public ILocator CloneInput()
        => page.GetByTestId("switch-clone-string-value-modal-input");

    public ILocator CloneAccept()
        => page.GetByTestId("switch-clone-string-value-modal-submit");

    public ILocator CloneCancel()
        => page.GetByTestId("switch-clone-string-value-modal-cancel");

    // -------------------------------------------------
    // Navigation helpers
    // -------------------------------------------------

    public async Task OpenSwitchAsync(string name)
    {
        await OpenLink(name).ClickAsync();
        await page.WaitForURLAsync($"**/resources/hardware/{name}");
    }

    // -------------------------------------------------
    // Actions
    // -------------------------------------------------

    public async Task DeleteSwitchAsync(string name)
    {
        await DeleteButton(name).ClickAsync();
        await DeleteConfirmConfirmButton().ClickAsync();

        await Assertions.Expect(SwitchItem(name)).Not.ToBeVisibleAsync();
    }

    public async Task RenameSwitchAsync(string currentName, string newName)
    {
        await RenameButton(currentName).ClickAsync();

        await Assertions.Expect(RenameModal()).ToBeVisibleAsync();
        await RenameInput().FillAsync(newName);
        await RenameAccept().ClickAsync();

        await page.WaitForURLAsync($"**/resources/hardware/{newName}");
        await Assertions.Expect(SwitchItem(newName)).ToBeVisibleAsync();
    }

    public async Task CloneSwitchAsync(string currentName, string cloneName)
    {
        await CloneButton(currentName).ClickAsync();

        await Assertions.Expect(CloneModal()).ToBeVisibleAsync();
        await CloneInput().FillAsync(cloneName);
        await CloneAccept().ClickAsync();

        await page.WaitForURLAsync($"**/resources/hardware/{cloneName}");
        await Assertions.Expect(SwitchItem(cloneName)).ToBeVisibleAsync();
    }

    public async Task EnterEditModeAsync(string name)
    {
        await EditButton(name).ClickAsync();
        await Assertions.Expect(ModelInput(name)).ToBeVisibleAsync();
        await Assertions.Expect(ManagedCheckbox(name)).ToBeVisibleAsync();
        await Assertions.Expect(PoeCheckbox(name)).ToBeVisibleAsync();
    }

    public async Task SaveEditsAsync(string name)
    {
        await SaveButton(name).ClickAsync();

        // back to view mode
        await Assertions.Expect(EditButton(name)).ToBeVisibleAsync();
    }

    public async Task CancelEditsAsync(string name)
    {
        await CancelButton(name).ClickAsync();

        // back to view mode
        await Assertions.Expect(EditButton(name)).ToBeVisibleAsync();
    }

    private static string Sanitize(string value) => value.Replace(" ", "-");
}
