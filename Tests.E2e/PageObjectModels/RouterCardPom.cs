namespace Tests.E2e.PageObjectModels;

using Microsoft.Playwright;

public class RouterCardPom(IPage page)
{
    public TagsPom Tags => new(page);

    // -------------------------------------------------
    // Dynamic Router Item (root)
    // -------------------------------------------------

    public ILocator RouterItem(string name)
        => page.GetByTestId($"router-item-{Sanitize(name)}");

    public ILocator OpenLink(string name)
        => page.GetByTestId($"router-item-{Sanitize(name)}-link");
    
    // -------------------------------------------------
    // Header Actions
    // -------------------------------------------------

    public ILocator EditButton(string name)
        => RouterItem(name).GetByTestId("edit-router-button");

    public ILocator SaveButton(string name)
        => RouterItem(name).GetByTestId("save-router-button");

    public ILocator CancelButton(string name)
        => RouterItem(name).GetByTestId("cancel-router-button");

    public ILocator RenameButton(string name)
        => RouterItem(name).GetByTestId("rename-router-button");

    public ILocator CloneButton(string name)
        => RouterItem(name).GetByTestId("clone-router-button");

    public ILocator DeleteButton(string name)
        => RouterItem(name).GetByTestId("delete-router-button");

    // -------------------------------------------------
    // Model
    // -------------------------------------------------

    public ILocator ModelSection(string name)
        => RouterItem(name).GetByTestId("router-model-section");

    public ILocator ModelInput(string name)
        => RouterItem(name).GetByTestId("router-model-input");

    public ILocator ModelValue(string name)
        => RouterItem(name).GetByTestId("router-model-value");

    // -------------------------------------------------
    // Features
    // -------------------------------------------------

    public ILocator FeaturesSection(string name)
        => RouterItem(name).GetByTestId("router-features-section");

    public ILocator ManagedCheckbox(string name)
        => RouterItem(name).GetByTestId("router-managed-checkbox");

    public ILocator PoeCheckbox(string name)
        => RouterItem(name).GetByTestId("router-poe-checkbox");

    public ILocator ManagedBadge(string name)
        => RouterItem(name).GetByTestId("router-managed-badge");

    public ILocator PoeBadge(string name)
        => RouterItem(name).GetByTestId("router-poe-badge");

    // -------------------------------------------------
    // Ports
    // -------------------------------------------------

    public ILocator PortsSection(string name)
        => RouterItem(name).GetByTestId("router-ports-section");

    public ILocator AddPortButton(string name)
        => RouterItem(name).GetByTestId("add-port-button");

    public ILocator EditPortButton(string routerName, string portType, double portSpeed)
        => RouterItem(routerName).GetByTestId($"edit-port-{portType}-{portSpeed}");

    // -------------------------------------------------
    // Notes (Markdown)
    // -------------------------------------------------

    public ILocator NotesSection(string name)
        => RouterItem(name).GetByTestId("router-notes-section");

    // MarkdownViewer / MarkdownEditor now use TestIdPrefix internally
    public ILocator NotesViewerRoot(string name)
        => RouterItem(name).GetByTestId("router-notes-viewer");

    public ILocator NotesViewerContent(string name)
        => RouterItem(name).GetByTestId("router-notes-viewer-content");

    public ILocator NotesEditorRoot(string name)
        => RouterItem(name).GetByTestId("router-notes-editor");

    public ILocator NotesEditorTextarea(string name)
        => RouterItem(name).GetByTestId("router-notes-editor-textarea");

    public ILocator NotesEditorSave(string name)
        => RouterItem(name).GetByTestId("router-notes-editor-save");

    public ILocator NotesEditorCancel(string name)
        => RouterItem(name).GetByTestId("router-notes-editor-cancel");

    // -------------------------------------------------
    // Modals
    // -------------------------------------------------

    public ILocator DeleteConfirmConfirmButton()
        => page.GetByTestId("Router-confirm-modal-confirm");

    public ILocator DeleteConfirmCancelButton()
        => page.GetByTestId("Router-confirm-modal-cancel");

    // StringValueModal (rename)
    public ILocator RenameModal()
        => page.GetByTestId("router-rename-string-value-modal");

    public ILocator RenameInput()
        => page.GetByTestId("router-rename-string-value-modal-input");

    public ILocator RenameAccept()
        => page.GetByTestId("router-rename-string-value-modal-submit");

    public ILocator RenameCancel()
        => page.GetByTestId("router-rename-string-value-modal-cancel");

    // StringValueModal (clone)
    public ILocator CloneModal()
        => page.GetByTestId("router-clone-string-value-modal");

    public ILocator CloneInput()
        => page.GetByTestId("router-clone-string-value-modal-input");

    public ILocator CloneAccept()
        => page.GetByTestId("router-clone-string-value-modal-submit");

    public ILocator CloneCancel()
        => page.GetByTestId("router-clone-string-value-modal-cancel");

    // -------------------------------------------------
    // Navigation helpers
    // -------------------------------------------------

    public async Task OpenRouterAsync(string name)
    {
        await OpenLink(name).ClickAsync();
        await page.WaitForURLAsync($"**/resources/hardware/{name}");
    }

    // -------------------------------------------------
    // Actions
    // -------------------------------------------------

    public async Task DeleteRouterAsync(string name)
    {
        await DeleteButton(name).ClickAsync();
        await DeleteConfirmConfirmButton().ClickAsync();

        await Assertions.Expect(RouterItem(name)).Not.ToBeVisibleAsync();
    }

    public async Task RenameRouterAsync(string currentName, string newName)
    {
        await RenameButton(currentName).ClickAsync();

        await Assertions.Expect(RenameModal()).ToBeVisibleAsync();
        await RenameInput().FillAsync(newName);
        await RenameAccept().ClickAsync();

        await page.WaitForURLAsync($"**/resources/hardware/{newName}");
        await Assertions.Expect(RouterItem(newName)).ToBeVisibleAsync();
    }

    public async Task CloneRouterAsync(string currentName, string cloneName)
    {
        await CloneButton(currentName).ClickAsync();

        await Assertions.Expect(CloneModal()).ToBeVisibleAsync();
        await CloneInput().FillAsync(cloneName);
        await CloneAccept().ClickAsync();

        await page.WaitForURLAsync($"**/resources/hardware/{cloneName}");
        await Assertions.Expect(RouterItem(cloneName)).ToBeVisibleAsync();
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
