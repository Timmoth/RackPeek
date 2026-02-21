namespace Tests.E2e.PageObjectModels;

using Microsoft.Playwright;

public class DesktopCardPom(IPage page)
{    
    public TagsPom Tags => new(page);

    // -------------------------------------------------
    // Root + Navigation
    // -------------------------------------------------

    public ILocator DesktopItem(string name)
        => page.GetByTestId($"desktop-item-{Sanitize(name)}");

    public ILocator OpenDesktopLink(string name)
        => page.GetByTestId($"desktop-item-{Sanitize(name)}-link");

    public async Task OpenDesktopAsync(string name)
    {
        await OpenDesktopLink(name).ClickAsync();
        await page.WaitForURLAsync($"**/resources/hardware/{name}");
    }

    // -------------------------------------------------
    // Header actions
    // -------------------------------------------------

    public ILocator RenameButton(string name)
        => DesktopItem(name).GetByTestId("rename-desktop-button");

    public ILocator CloneButton(string name)
        => DesktopItem(name).GetByTestId("clone-desktop-button");

    public ILocator DeleteButton(string name)
        => DesktopItem(name).GetByTestId("delete-desktop-button");

    public ILocator ModelBadge(string name)
        => DesktopItem(name).GetByTestId("desktop-model-badge");

    // -------------------------------------------------
    // CPU section
    // -------------------------------------------------

    public ILocator CpuSection(string name)
        => DesktopItem(name).GetByTestId("desktop-cpu-section");

    public ILocator AddCpuButton(string name)
        => DesktopItem(name).GetByTestId("add-cpu-button");

    public ILocator EditCpuButton(string name, string cpuToString)
        => DesktopItem(name).GetByTestId($"edit-cpu-{Sanitize(cpuToString)}");

    // -------------------------------------------------
    // RAM section
    // -------------------------------------------------

    public ILocator RamSection(string name)
        => DesktopItem(name).GetByTestId("desktop-ram-section");

    public ILocator EditRamButton(string name)
        => DesktopItem(name).GetByTestId("edit-ram-button");

    public ILocator RamValueButton(string name)
        => DesktopItem(name).GetByTestId("ram-value-button");

    // -------------------------------------------------
    // Drive section
    // -------------------------------------------------

    public ILocator DriveSection(string name)
        => DesktopItem(name).GetByTestId("desktop-drive-section");

    public ILocator AddDriveButton(string name)
        => DesktopItem(name).GetByTestId("add-drive-button");

    public ILocator EditDriveButton(string name, string type, int size)
        => DesktopItem(name).GetByTestId($"edit-drive-{type}-{size}");

    // -------------------------------------------------
    // NIC section
    // -------------------------------------------------

    public ILocator NicSection(string name)
        => DesktopItem(name).GetByTestId("desktop-nic-section");

    public ILocator AddNicButton(string name)
        => DesktopItem(name).GetByTestId("add-nic-button");

    public ILocator EditNicButton(string name, string type, double speed)
        => DesktopItem(name).GetByTestId($"edit-nic-{type}-{speed}");

    // -------------------------------------------------
    // GPU section
    // -------------------------------------------------

    public ILocator GpuSection(string name)
        => DesktopItem(name).GetByTestId("desktop-gpu-section");

    public ILocator AddGpuButton(string name)
        => DesktopItem(name).GetByTestId("add-gpu-button");

    public ILocator EditGpuButton(string name, string model, int vram)
        => DesktopItem(name).GetByTestId($"edit-gpu-{model}-{vram}");

    // -------------------------------------------------
    // Notes (MarkdownViewer/MarkdownEditor use prefixes)
    // -------------------------------------------------

    public ILocator NotesSection(string name)
        => DesktopItem(name).GetByTestId("desktop-notes-section");

    // MarkdownViewer (TestIdPrefix="desktop-notes-viewer")
    public ILocator NotesViewerRoot(string name)
        => DesktopItem(name).GetByTestId("desktop-notes-viewer-markdown-viewer");

    public ILocator NotesViewerEditButton(string name)
        => DesktopItem(name).GetByTestId("desktop-notes-viewer-markdown-viewer-edit-button");

    // MarkdownEditor (TestIdPrefix="desktop-notes-editor")
    public ILocator NotesEditorRoot(string name)
        => DesktopItem(name).GetByTestId("desktop-notes-editor-markdown-editor");

    public ILocator NotesEditorTextarea(string name)
        => DesktopItem(name).GetByTestId("desktop-notes-editor-markdown-editor-textarea");

    public ILocator NotesEditorSave(string name)
        => DesktopItem(name).GetByTestId("desktop-notes-editor-markdown-editor-save");

    public ILocator NotesEditorCancel(string name)
        => DesktopItem(name).GetByTestId("desktop-notes-editor-markdown-editor-cancel");

    // -------------------------------------------------
    // Modals
    // -------------------------------------------------

    // ConfirmModal TestIdPrefix="Desktop" => "Desktop-confirm-modal-*"
    public ILocator DeleteConfirmModal => page.GetByTestId("Desktop-confirm-modal");
    public ILocator DeleteConfirmButton => page.GetByTestId("Desktop-confirm-modal-confirm");
    public ILocator DeleteCancelButton => page.GetByTestId("Desktop-confirm-modal-cancel");

    // StringValueModal prefixes you set:
    // desktop-rename => "desktop-rename-string-value-modal-*"
    public ILocator RenameModal => page.GetByTestId("desktop-rename-string-value-modal");
    public ILocator RenameModalInput => page.GetByTestId("desktop-rename-string-value-modal-input");
    public ILocator RenameModalAccept => page.GetByTestId("desktop-rename-string-value-modal-submit");
    public ILocator RenameModalCancel => page.GetByTestId("desktop-rename-string-value-modal-cancel");

    // desktop-clone => "desktop-clone-string-value-modal-*"
    public ILocator CloneModal => page.GetByTestId("desktop-clone-string-value-modal");
    public ILocator CloneModalInput => page.GetByTestId("desktop-clone-string-value-modal-input");
    public ILocator CloneModalAccept => page.GetByTestId("desktop-clone-string-value-modal-submit");
    public ILocator CloneModalCancel => page.GetByTestId("desktop-clone-string-value-modal-cancel");

    // -------------------------------------------------
    // Actions helpers
    // -------------------------------------------------

    public async Task DeleteDesktopAsync(string name)
    {
        await DeleteButton(name).ClickAsync();
        await Assertions.Expect(DeleteConfirmModal).ToBeVisibleAsync();
        await DeleteConfirmButton.ClickAsync();
        await Assertions.Expect(DesktopItem(name)).Not.ToBeVisibleAsync();
    }

    public async Task RenameDesktopAsync(string currentName, string newName)
    {
        await RenameButton(currentName).ClickAsync();
        await Assertions.Expect(RenameModal).ToBeVisibleAsync();

        await RenameModalInput.FillAsync(newName);
        await RenameModalAccept.ClickAsync();

        await page.WaitForURLAsync($"**/resources/hardware/{newName}");
    }

    public async Task CloneDesktopAsync(string currentName, string cloneName)
    {
        await CloneButton(currentName).ClickAsync();
        await Assertions.Expect(CloneModal).ToBeVisibleAsync();

        await CloneModalInput.FillAsync(cloneName);
        await CloneModalAccept.ClickAsync();

        await page.WaitForURLAsync($"**/resources/hardware/{cloneName}");
    }

    private static string Sanitize(string value)
        => value.Replace(" ", "-");
}
