namespace Tests.E2e.PageObjectModels;

using Microsoft.Playwright;

public class LaptopCardPom(IPage page)
{
    public TagsPom Tags => new(page);

    // -------------------------------------------------
    // Root + Navigation
    // -------------------------------------------------

    public ILocator LaptopItem(string name)
        => page.GetByTestId($"laptop-item-{Sanitize(name)}");

    public ILocator OpenLaptopLink(string name)
        => page.GetByTestId($"laptop-item-{Sanitize(name)}-link");

    public async Task OpenLaptopAsync(string name)
    {
        await OpenLaptopLink(name).ClickAsync();
        await page.WaitForURLAsync($"**/resources/hardware/{name}");
    }

    // -------------------------------------------------
    // Header actions
    // -------------------------------------------------

    public ILocator RenameButton(string name)
        => LaptopItem(name).GetByTestId("rename-laptop-button");

    public ILocator CloneButton(string name)
        => LaptopItem(name).GetByTestId("clone-laptop-button");

    public ILocator DeleteButton(string name)
        => LaptopItem(name).GetByTestId("delete-laptop-button");

    public ILocator ModelBadge(string name)
        => LaptopItem(name).GetByTestId("laptop-model-badge");

    // -------------------------------------------------
    // CPU section
    // -------------------------------------------------

    public ILocator CpuSection(string name)
        => LaptopItem(name).GetByTestId("laptop-cpu-section");

    public ILocator AddCpuButton(string name)
        => LaptopItem(name).GetByTestId("add-cpu-button");

    public ILocator EditCpuButton(string name, string cpuToString)
        => LaptopItem(name).GetByTestId($"edit-cpu-{Sanitize(cpuToString)}");

    // -------------------------------------------------
    // RAM section
    // -------------------------------------------------

    public ILocator RamSection(string name)
        => LaptopItem(name).GetByTestId("laptop-ram-section");

    public ILocator EditRamButton(string name)
        => LaptopItem(name).GetByTestId("edit-ram-button");

    public ILocator RamValueButton(string name)
        => LaptopItem(name).GetByTestId("ram-value-button");

    // -------------------------------------------------
    // Drive section
    // -------------------------------------------------

    public ILocator DriveSection(string name)
        => LaptopItem(name).GetByTestId("laptop-drive-section");

    public ILocator AddDriveButton(string name)
        => LaptopItem(name).GetByTestId("add-drive-button");

    public ILocator EditDriveButton(string name, string type, int size)
        => LaptopItem(name).GetByTestId($"edit-drive-{type}-{size}");

    // -------------------------------------------------
    // NIC section
    // -------------------------------------------------

    public ILocator NicSection(string name)
        => LaptopItem(name).GetByTestId("laptop-nic-section");

    public ILocator AddNicButton(string name)
        => LaptopItem(name).GetByTestId("add-nic-button");

    public ILocator EditNicButton(string name, string type, double speed)
        => LaptopItem(name).GetByTestId($"edit-nic-{type}-{speed}");

    // -------------------------------------------------
    // GPU section
    // -------------------------------------------------

    public ILocator GpuSection(string name)
        => LaptopItem(name).GetByTestId("laptop-gpu-section");

    public ILocator AddGpuButton(string name)
        => LaptopItem(name).GetByTestId("add-gpu-button");

    public ILocator EditGpuButton(string name, string model, int vram)
        => LaptopItem(name).GetByTestId($"edit-gpu-{model}-{vram}");

    // -------------------------------------------------
    // Notes (MarkdownViewer/MarkdownEditor use prefixes)
    // -------------------------------------------------

    public ILocator NotesSection(string name)
        => LaptopItem(name).GetByTestId("laptop-notes-section");

    // MarkdownViewer (TestIdPrefix="laptop-notes-viewer")
    public ILocator NotesViewerRoot(string name)
        => LaptopItem(name).GetByTestId("laptop-notes-viewer-markdown-viewer");

    public ILocator NotesViewerEditButton(string name)
        => LaptopItem(name).GetByTestId("laptop-notes-viewer-markdown-viewer-edit-button");

    // MarkdownEditor (TestIdPrefix="laptop-notes-editor")
    public ILocator NotesEditorRoot(string name)
        => LaptopItem(name).GetByTestId("laptop-notes-editor-markdown-editor");

    public ILocator NotesEditorTextarea(string name)
        => LaptopItem(name).GetByTestId("laptop-notes-editor-markdown-editor-textarea");

    public ILocator NotesEditorSave(string name)
        => LaptopItem(name).GetByTestId("laptop-notes-editor-markdown-editor-save");

    public ILocator NotesEditorCancel(string name)
        => LaptopItem(name).GetByTestId("laptop-notes-editor-markdown-editor-cancel");

    // -------------------------------------------------
    // Modals
    // -------------------------------------------------

    // ConfirmModal TestIdPrefix="Laptop" => "Laptop-confirm-modal-*"
    public ILocator DeleteConfirmModal => page.GetByTestId("Laptop-confirm-modal");
    public ILocator DeleteConfirmButton => page.GetByTestId("Laptop-confirm-modal-confirm");
    public ILocator DeleteCancelButton => page.GetByTestId("Laptop-confirm-modal-cancel");

    // StringValueModal prefixes you set:
    // laptop-rename => "laptop-rename-string-value-modal-*"
    public ILocator RenameModal => page.GetByTestId("laptop-rename-string-value-modal");
    public ILocator RenameModalInput => page.GetByTestId("laptop-rename-string-value-modal-input");
    public ILocator RenameModalAccept => page.GetByTestId("laptop-rename-string-value-modal-submit");
    public ILocator RenameModalCancel => page.GetByTestId("laptop-rename-string-value-modal-cancel");

    // laptop-clone => "laptop-clone-string-value-modal-*"
    public ILocator CloneModal => page.GetByTestId("laptop-clone-string-value-modal");
    public ILocator CloneModalInput => page.GetByTestId("laptop-clone-string-value-modal-input");
    public ILocator CloneModalAccept => page.GetByTestId("laptop-clone-string-value-modal-submit");
    public ILocator CloneModalCancel => page.GetByTestId("laptop-clone-string-value-modal-cancel");

    // -------------------------------------------------
    // Actions helpers
    // -------------------------------------------------

    public async Task DeleteLaptopAsync(string name)
    {
        await DeleteButton(name).ClickAsync();
        await Assertions.Expect(DeleteConfirmModal).ToBeVisibleAsync();
        await DeleteConfirmButton.ClickAsync();
        await Assertions.Expect(LaptopItem(name)).Not.ToBeVisibleAsync();
    }

    public async Task RenameLaptopAsync(string currentName, string newName)
    {
        await RenameButton(currentName).ClickAsync();
        await Assertions.Expect(RenameModal).ToBeVisibleAsync();

        await RenameModalInput.FillAsync(newName);
        await RenameModalAccept.ClickAsync();

        await page.WaitForURLAsync($"**/resources/hardware/{newName}");
    }

    public async Task CloneLaptopAsync(string currentName, string cloneName)
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
