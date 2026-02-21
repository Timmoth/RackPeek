using Microsoft.Playwright;

namespace Tests.E2e.PageObjectModels;

public class ServerCardPom(IPage page)
{
    public TagsPom Tags => new(page);

    // -------------------------------------------------
    // Root / Identity
    // -------------------------------------------------

    public ILocator ServerItem(string name)
        => page.GetByTestId($"server-item-{Sanitize(name)}");

    public ILocator ServerLink(string name)
        => page.GetByTestId($"server-item-{Sanitize(name)}-link");

    public async Task AssertVisibleAsync(string name)
        => await Assertions.Expect(ServerItem(name)).ToBeVisibleAsync();

    // -------------------------------------------------
    // Top actions
    // -------------------------------------------------

    public ILocator RenameButton(string name)
        => ServerItem(name).GetByTestId("rename-server-button");

    public ILocator CloneButton(string name)
        => ServerItem(name).GetByTestId("clone-server-button");

    public ILocator DeleteButton(string name)
        => ServerItem(name).GetByTestId("delete-server-button");

    // -------------------------------------------------
    // CPU section + modal (TestIdPrefix="server-cpu")
    // -------------------------------------------------

    public ILocator CpuSection(string name)
        => ServerItem(name).GetByTestId("server-cpu-section");

    public ILocator AddCpuButton(string name)
        => ServerItem(name).GetByTestId("add-cpu-button");

    public ILocator EditCpuButton(string name, string cpuDisplayKey)
        => ServerItem(name).GetByTestId($"edit-cpu-{Sanitize(cpuDisplayKey)}");

    // CpuModal base id becomes: "server-cpu-cpu-modal"
    public ILocator CpuModalRoot => page.GetByTestId("server-cpu-cpu-modal");
    public ILocator CpuModalModelInput => page.GetByTestId("server-cpu-cpu-modal-model-input");
    public ILocator CpuModalCoresInput => page.GetByTestId("server-cpu-cpu-modal-cores-input");
    public ILocator CpuModalThreadsInput => page.GetByTestId("server-cpu-cpu-modal-threads-input");
    public ILocator CpuModalSubmit => page.GetByTestId("server-cpu-cpu-modal-submit");
    public ILocator CpuModalCancel => page.GetByTestId("server-cpu-cpu-modal-cancel");
    public ILocator CpuModalDelete => page.GetByTestId("server-cpu-cpu-modal-delete");
    

    public ILocator RamModalRoot => page.GetByTestId("server-ram-ram-modal");
    public ILocator RamModalSizeInput => page.GetByTestId("server-ram-ram-modal-size-input");
    public ILocator RamModalMtsInput => page.GetByTestId("server-ram-ram-modal-mts-input");
    public ILocator RamModalSubmit => page.GetByTestId("server-ram-ram-modal-submit");
    public ILocator RamModalCancel => page.GetByTestId("server-ram-ram-modal-cancel");

    // -------------------------------------------------
    // Drive modal (TestIdPrefix="server-drive")
    // -------------------------------------------------

    public ILocator DriveModalRoot => page.GetByTestId("server-drive-drive-modal");
    public ILocator DriveModalTypeInput => page.GetByTestId("server-drive-drive-modal-type-input");
    public ILocator DriveModalSizeInput => page.GetByTestId("server-drive-drive-modal-size-input");
    public ILocator DriveModalSubmit => page.GetByTestId("server-drive-drive-modal-submit");
    public ILocator DriveModalCancel => page.GetByTestId("server-drive-drive-modal-cancel");
    public ILocator DriveModalDelete => page.GetByTestId("server-drive-drive-modal-delete");

    // -------------------------------------------------
    // NIC modal (TestIdPrefix="server-nic")
    // -------------------------------------------------

    public ILocator NicModalRoot => page.GetByTestId("server-nic-nic-modal");
    public ILocator NicModalTypeInput => page.GetByTestId("server-nic-nic-modal-type-input");
    public ILocator NicModalSpeedInput => page.GetByTestId("server-nic-nic-modal-speed-input");
    public ILocator NicModalPortsInput => page.GetByTestId("server-nic-nic-modal-ports-input");
    public ILocator NicModalSubmit => page.GetByTestId("server-nic-nic-modal-submit");
    public ILocator NicModalCancel => page.GetByTestId("server-nic-nic-modal-cancel");
    public ILocator NicModalDelete => page.GetByTestId("server-nic-nic-modal-delete");

    // -------------------------------------------------
    // GPU modal (TestIdPrefix="server-gpu")
    // -------------------------------------------------

    public ILocator GpuModalRoot => page.GetByTestId("server-gpu-gpu-modal");
    public ILocator GpuModalModelInput => page.GetByTestId("server-gpu-gpu-modal-model-input");
    public ILocator GpuModalVramInput => page.GetByTestId("server-gpu-gpu-modal-vram-input");
    public ILocator GpuModalSubmit => page.GetByTestId("server-gpu-gpu-modal-submit");
    public ILocator GpuModalCancel => page.GetByTestId("server-gpu-gpu-modal-cancel");
    public ILocator GpuModalDelete => page.GetByTestId("server-gpu-gpu-modal-delete");

    // -------------------------------------------------
    // Notes (TestIdPrefix="server-markdown")
    // MarkdownViewer base id: "server-markdown-markdown-viewer"
    // MarkdownEditor base id: "server-markdown-markdown-editor"
    // -------------------------------------------------

    public ILocator NotesViewerRoot => page.GetByTestId("server-markdown-markdown-viewer");
    public ILocator NotesViewerEditButton => page.GetByTestId("server-markdown-markdown-viewer-edit");

    public ILocator NotesEditorRoot => page.GetByTestId("server-markdown-markdown-editor");
    public ILocator NotesEditorTextarea => page.GetByTestId("server-markdown-markdown-editor-textarea");
    public ILocator NotesEditorSave => page.GetByTestId("server-markdown-markdown-editor-save");
    public ILocator NotesEditorCancel => page.GetByTestId("server-markdown-markdown-editor-cancel");

    // -------------------------------------------------
    // Delete confirm modal (TestIdPrefix="server-delete")
    // ConfirmModal base id becomes: "server-delete-confirm-modal"
    // -------------------------------------------------

    public ILocator DeleteConfirmModal => page.GetByTestId("server-delete-confirm-modal");
    public ILocator DeleteConfirm => page.GetByTestId("server-delete-confirm-modal-confirm");
    public ILocator DeleteCancel => page.GetByTestId("server-delete-confirm-modal-cancel");
    

    public ILocator RenameModal => page.GetByTestId("server-rename-string-value-modal");
    public ILocator RenameInput => page.GetByTestId("server-rename-string-value-modal-input");
    public ILocator RenameAccept => page.GetByTestId("server-rename-string-value-modal-submit");
    public ILocator RenameCancel => page.GetByTestId("server-rename-string-value-modal-cancel");

    // -------------------------------------------------
    // Clone modal (TestIdPrefix="server-clone")
    // -------------------------------------------------

    public ILocator CloneModal => page.GetByTestId("server-clone-string-value-modal");
    public ILocator CloneInput => page.GetByTestId("server-clone-string-value-modal-input");
    public ILocator CloneAccept => page.GetByTestId("server-clone-string-value-modal-submit");
    public ILocator CloneCancel => page.GetByTestId("server-clone-string-value-modal-cancel");

    // -------------------------------------------------
    // Helpers / Common actions
    // -------------------------------------------------

    public async Task RenameAsync(string currentName, string newName)
    {
        await RenameButton(currentName).ClickAsync();
        await Assertions.Expect(RenameModal).ToBeVisibleAsync();
        await RenameInput.FillAsync(newName);
        await RenameAccept.ClickAsync();
        await page.WaitForURLAsync($"**/resources/hardware/{newName}");
    }

    public async Task CloneAsync(string currentName, string cloneName)
    {
        await CloneButton(currentName).ClickAsync();
        await Assertions.Expect(CloneModal).ToBeVisibleAsync();
        await CloneInput.FillAsync(cloneName);
        await CloneAccept.ClickAsync();
        await page.WaitForURLAsync($"**/resources/hardware/{cloneName}");
    }

    public async Task DeleteAsync(string name)
    {
        await DeleteButton(name).ClickAsync();
        await Assertions.Expect(DeleteConfirmModal).ToBeVisibleAsync();
        await DeleteConfirm.ClickAsync();
    }

    private static string Sanitize(string value)
        => value.Replace(" ", "-");
}
