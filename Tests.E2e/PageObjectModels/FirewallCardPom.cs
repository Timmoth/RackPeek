namespace Tests.E2e.PageObjectModels;

using Microsoft.Playwright;

public class FirewallCardPom(IPage page)
{
    public TagsPom Tags => new(page);

    // -------------------------------------------------
    // Dynamic Firewall Item (root)
    // -------------------------------------------------

    public ILocator FirewallItem(string name)
        => page.GetByTestId($"firewall-item-{Sanitize(name)}");

    public ILocator OpenLink(string name)
        => FirewallItem(name).GetByTestId($"firewall-item-{Sanitize(name)}-link");

    // -------------------------------------------------
    // Header Actions
    // -------------------------------------------------

    public ILocator EditButton(string name)
        => FirewallItem(name).GetByTestId("edit-firewall-button");

    public ILocator SaveButton(string name)
        => FirewallItem(name).GetByTestId("save-firewall-button");

    public ILocator CancelButton(string name)
        => FirewallItem(name).GetByTestId("cancel-firewall-button");

    public ILocator RenameButton(string name)
        => FirewallItem(name).GetByTestId("rename-firewall-button");

    public ILocator CloneButton(string name)
        => FirewallItem(name).GetByTestId("clone-firewall-button");

    public ILocator DeleteButton(string name)
        => FirewallItem(name).GetByTestId("delete-firewall-button");

    // -------------------------------------------------
    // Model
    // -------------------------------------------------

    public ILocator ModelSection(string name)
        => FirewallItem(name).GetByTestId("firewall-model-section");

    public ILocator ModelInput(string name)
        => FirewallItem(name).GetByTestId("firewall-model-input");

    public ILocator ModelValue(string name)
        => FirewallItem(name).GetByTestId("firewall-model-value");

    // -------------------------------------------------
    // Features
    // -------------------------------------------------

    public ILocator FeaturesSection(string name)
        => FirewallItem(name).GetByTestId("firewall-features-section");

    public ILocator ManagedCheckbox(string name)
        => FirewallItem(name).GetByTestId("firewall-managed-checkbox");

    public ILocator PoeCheckbox(string name)
        => FirewallItem(name).GetByTestId("firewall-poe-checkbox");

    public ILocator ManagedBadge(string name)
        => FirewallItem(name).GetByTestId("firewall-managed-badge");

    public ILocator PoeBadge(string name)
        => FirewallItem(name).GetByTestId("firewall-poe-badge");

    // -------------------------------------------------
    // Ports
    // -------------------------------------------------

    public ILocator PortsSection(string name)
        => FirewallItem(name).GetByTestId("firewall-ports-section");

    public ILocator AddPortButton(string name)
        => FirewallItem(name).GetByTestId("add-port-button");

    public ILocator EditPortButton(string firewallName, string portType, double portSpeed)
        => FirewallItem(firewallName).GetByTestId($"edit-port-{portType}-{portSpeed}");

    // -------------------------------------------------
    // Notes (Markdown)
    // -------------------------------------------------

    public ILocator NotesSection(string name)
        => FirewallItem(name).GetByTestId("firewall-notes-section");

    // MarkdownViewer / MarkdownEditor now use TestIdPrefix internally
    public ILocator NotesViewerRoot(string name)
        => FirewallItem(name).GetByTestId("firewall-notes-viewer");

    public ILocator NotesViewerContent(string name)
        => FirewallItem(name).GetByTestId("firewall-notes-viewer-content");

    public ILocator NotesEditorRoot(string name)
        => FirewallItem(name).GetByTestId("firewall-notes-editor");

    public ILocator NotesEditorTextarea(string name)
        => FirewallItem(name).GetByTestId("firewall-notes-editor-textarea");

    public ILocator NotesEditorSave(string name)
        => FirewallItem(name).GetByTestId("firewall-notes-editor-save");

    public ILocator NotesEditorCancel(string name)
        => FirewallItem(name).GetByTestId("firewall-notes-editor-cancel");

    // -------------------------------------------------
    // Modals
    // -------------------------------------------------

    public ILocator DeleteConfirmConfirmButton()
        => page.GetByTestId("Firewall-confirm-modal-confirm");

    public ILocator DeleteConfirmCancelButton()
        => page.GetByTestId("Firewall-confirm-modal-cancel");

    // StringValueModal (rename)
    public ILocator RenameModal()
        => page.GetByTestId("firewall-rename-string-value-modal");

    public ILocator RenameInput()
        => page.GetByTestId("firewall-rename-string-value-modal-input");

    public ILocator RenameAccept()
        => page.GetByTestId("firewall-rename-string-value-modal-submit");

    public ILocator RenameCancel()
        => page.GetByTestId("firewall-rename-string-value-modal-cancel");

    // StringValueModal (clone)
    public ILocator CloneModal()
        => page.GetByTestId("firewall-clone-string-value-modal");

    public ILocator CloneInput()
        => page.GetByTestId("firewall-clone-string-value-modal-input");

    public ILocator CloneAccept()
        => page.GetByTestId("firewall-clone-string-value-modal-submit");

    public ILocator CloneCancel()
        => page.GetByTestId("firewall-clone-string-value-modal-cancel");

    // -------------------------------------------------
    // Navigation helpers
    // -------------------------------------------------

    public async Task OpenFirewallAsync(string name)
    {
        await OpenLink(name).ClickAsync();
        await page.WaitForURLAsync($"**/resources/hardware/{name}");
    }

    // -------------------------------------------------
    // Actions
    // -------------------------------------------------

    public async Task DeleteFirewallAsync(string name)
    {
        await DeleteButton(name).ClickAsync();
        await DeleteConfirmConfirmButton().ClickAsync();

        await Assertions.Expect(FirewallItem(name)).Not.ToBeVisibleAsync();
    }

    public async Task RenameFirewallAsync(string currentName, string newName)
    {
        await RenameButton(currentName).ClickAsync();

        await Assertions.Expect(RenameModal()).ToBeVisibleAsync();
        await RenameInput().FillAsync(newName);
        await RenameAccept().ClickAsync();

        await page.WaitForURLAsync($"**/resources/hardware/{newName}");
        await Assertions.Expect(FirewallItem(newName)).ToBeVisibleAsync();
    }

    public async Task CloneFirewallAsync(string currentName, string cloneName)
    {
        await CloneButton(currentName).ClickAsync();

        await Assertions.Expect(CloneModal()).ToBeVisibleAsync();
        await CloneInput().FillAsync(cloneName);
        await CloneAccept().ClickAsync();

        await page.WaitForURLAsync($"**/resources/hardware/{cloneName}");
        await Assertions.Expect(FirewallItem(cloneName)).ToBeVisibleAsync();
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
