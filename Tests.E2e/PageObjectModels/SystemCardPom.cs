using Microsoft.Playwright;

namespace Tests.E2e.PageObjectModels;

public class SystemCardPom(IPage page)
{
    public TagsPom Tags => new(page);

    // -------------------------------------------------
    // Helpers
    // -------------------------------------------------

    private static string Sanitize(string value)
        => value.Replace(" ", "-");

    public ILocator Card(string name)
        => page.GetByTestId($"system-item-{Sanitize(name)}");

    public ILocator Link(string name)
        => page.GetByTestId($"system-item-{Sanitize(name)}-link");

    // -------------------------------------------------
    // Action Buttons
    // -------------------------------------------------

    public ILocator EditButton(string name)
        => Card(name).GetByTestId("edit-system-button");

    public ILocator SaveButton(string name)
        => Card(name).GetByTestId("save-system-button");

    public ILocator CancelButton(string name)
        => Card(name).GetByTestId("cancel-system-button");

    public ILocator RenameButton(string name)
        => Card(name).GetByTestId("rename-system-button");

    public ILocator CloneButton(string name)
        => Card(name).GetByTestId("clone-system-button");

    public ILocator DeleteButton(string name)
        => Card(name).GetByTestId("delete-system-button");

    // -------------------------------------------------
    // Edit Inputs
    // -------------------------------------------------

    public ILocator TypeSelect(string name)
        => Card(name).GetByTestId("system-type-select");

    public ILocator OsInput(string name)
        => Card(name).GetByTestId("system-os-input");

    public ILocator CoresInput(string name)
        => Card(name).GetByTestId("system-cores-input");

    public ILocator RamInput(string name)
        => Card(name).GetByTestId("system-ram-input");

    public ILocator RunsOnButton(string name)
        => Card(name).GetByTestId("system-runs-on-button");

    // -------------------------------------------------
    // Drives
    // -------------------------------------------------

    public ILocator AddDriveButton(string name)
        => Card(name).GetByTestId("add-drive-button");

    public ILocator DriveItem(string name, string type, int size)
        => Card(name).GetByTestId($"drive-item-{type}-{size}");

    // ---- Drive Modal (TestIdPrefix = "system") ----

// ---- Drive Modal (TestIdPrefix = "system") ----

    public ILocator DriveTypeSelect
        => page.GetByTestId("system-drive-modal-type-input");
    public ILocator DriveSizeInput
        => page.GetByTestId("system-drive-modal-size-input");

    public ILocator DriveSubmitButton
        => page.GetByTestId("system-drive-modal-submit");

    public ILocator DriveDeleteButton
        => page.GetByTestId("system-delete-button");

    // High-level drive action
    public async Task AddDriveAsync(string name, string type, int size)
    {
        await AddDriveButton(name).ClickAsync();

        await DriveTypeSelect.SelectOptionAsync(new SelectOptionValue
        {
            Value = type
        });

        await DriveSizeInput.FillAsync(size.ToString());

        await DriveSubmitButton.ClickAsync();

        await Assertions.Expect(
            DriveItem(name, type, size)
        ).ToBeVisibleAsync();
    }

    // -------------------------------------------------
    // Notes
    // -------------------------------------------------

    public ILocator NotesViewer
        => page.GetByTestId("system-notes-viewer-container");

    public ILocator NotesEditor
        => page.GetByTestId("system-notes-editor-container");

    // -------------------------------------------------
    // Confirm Delete Modal
    // -------------------------------------------------

    public ILocator ConfirmDeleteButton
        => page.GetByTestId("system-delete-confirm-modal-confirm");

    // -------------------------------------------------
    // High-Level Actions
    // -------------------------------------------------

    public async Task AssertVisibleAsync(string name)
    {
        await Assertions.Expect(Card(name)).ToBeVisibleAsync();
    }

    public async Task BeginEditAsync(string name)
    {
        await EditButton(name).ClickAsync();
        await Assertions.Expect(SaveButton(name)).ToBeVisibleAsync();
    }

    public async Task SaveAsync(string name)
    {
        await SaveButton(name).ClickAsync();
        await Assertions.Expect(EditButton(name)).ToBeVisibleAsync();
    }

    public async Task CancelAsync(string name)
    {
        await CancelButton(name).ClickAsync();
        await Assertions.Expect(EditButton(name)).ToBeVisibleAsync();
    }

    public async Task RenameAsync(string currentName, string newName)
    {
        await RenameButton(currentName).ClickAsync();

        await page.GetByTestId("system-rename-string-value-modal-input")
            .FillAsync(newName);

        await page.GetByTestId("system-rename-string-value-modal-submit")
            .ClickAsync();

        await page.WaitForURLAsync($"**/resources/systems/{newName}");
    }

    public async Task CloneAsync(string currentName, string cloneName)
    {
        await CloneButton(currentName).ClickAsync();

        await page.GetByTestId("system-clone-string-value-modal-input")
            .FillAsync(cloneName);

        await page.GetByTestId("system-clone-string-value-modal-submit")
            .ClickAsync();

        await page.WaitForURLAsync($"**/resources/systems/{cloneName}");
    }

    public async Task DeleteAsync(string name)
    {
        await DeleteButton(name).ClickAsync();
        await ConfirmDeleteButton.ClickAsync();
    }
}
