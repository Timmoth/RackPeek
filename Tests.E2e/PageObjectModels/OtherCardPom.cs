using Microsoft.Playwright;

namespace Tests.E2e.PageObjectModels;

public class OtherCardPom(IPage page) {
    public TagsPom Tags => new(page);
    public LabelsPom Labels => new(page);

    public PortsPom Ports => new(page);

    private const string _portsPrefix = "other-ports";

    // -------------------------------------------------
    // Notes
    // -------------------------------------------------

    public ILocator NotesViewer
        => page.GetByTestId("other-notes-viewer-container");

    public ILocator NotesEditor
        => page.GetByTestId("other-notes-editor-container");

    // -------------------------------------------------
    // Confirm Modal (TestIdPrefix="Other")
    // -------------------------------------------------

    public ILocator ConfirmDeleteButton
        => page.GetByTestId("Other-confirm-modal-confirm");

    // -------------------------------------------------
    // Rename Modal (TestIdPrefix="other-rename")
    // -------------------------------------------------

    public ILocator RenameInput
        => page.GetByTestId("other-rename-string-value-modal-input");

    public ILocator RenameSubmit
        => page.GetByTestId("other-rename-string-value-modal-submit");

    // -------------------------------------------------
    // Clone Modal (TestIdPrefix="other-clone")
    // -------------------------------------------------

    public ILocator CloneInput
        => page.GetByTestId("other-clone-string-value-modal-input");

    public ILocator CloneSubmit
        => page.GetByTestId("other-clone-string-value-modal-submit");

    // -------------------------------------------------
    // Root
    // -------------------------------------------------

    private static string Sanitize(string value)
        => value.Replace(" ", "-");

    public ILocator Card(string name)
        => page.GetByTestId($"other-item-{Sanitize(name)}");

    public ILocator Link(string name)
        => page.GetByTestId($"other-item-{Sanitize(name)}-link");

    // -------------------------------------------------
    // Action Buttons
    // -------------------------------------------------

    public ILocator EditButton(string name)
        => Card(name).GetByTestId("edit-other-button");

    public ILocator SaveButton(string name)
        => Card(name).GetByTestId("save-other-button");

    public ILocator CancelButton(string name)
        => Card(name).GetByTestId("cancel-other-button");

    public ILocator RenameButton(string name)
        => Card(name).GetByTestId("rename-other-button");

    public ILocator CloneButton(string name)
        => Card(name).GetByTestId("clone-other-button");

    public ILocator DeleteButton(string name)
        => Card(name).GetByTestId("delete-other-button");

    // -------------------------------------------------
    // Edit Inputs
    // -------------------------------------------------

    public ILocator ModelInput(string name)
        => Card(name).GetByTestId("other-model-input");

    public ILocator DescriptionInput(string name)
        => Card(name).GetByTestId("other-description-input");

    // -------------------------------------------------
    // View Values
    // -------------------------------------------------

    public ILocator ModelValue(string name)
        => Card(name).GetByTestId("other-model-value");

    public ILocator DescriptionValue(string name)
        => Card(name).GetByTestId("other-description-value");

    // -------------------------------------------------
    // Assertions
    // -------------------------------------------------

    public async Task AssertVisibleAsync(string name) => await Assertions.Expect(Card(name)).ToBeVisibleAsync();

    // -------------------------------------------------
    // High-Level Actions
    // -------------------------------------------------

    public async Task BeginEditAsync(string name)
        => await EditButton(name).ClickAsync();

    public async Task SaveAsync(string name)
        => await SaveButton(name).ClickAsync();

    public async Task CancelAsync(string name)
        => await CancelButton(name).ClickAsync();

    public async Task RenameAsync(string currentName, string newName) {
        await RenameButton(currentName).ClickAsync();

        await RenameInput.FillAsync(newName);
        await RenameSubmit.ClickAsync();

        await page.WaitForURLAsync($"**/resources/hardware/{newName}");
    }

    public async Task CloneAsync(string currentName, string cloneName) {
        await CloneButton(currentName).ClickAsync();

        await CloneInput.FillAsync(cloneName);
        await CloneSubmit.ClickAsync();

        await page.WaitForURLAsync($"**/resources/hardware/{cloneName}");
    }

    public async Task DeleteAsync(string name) {
        await DeleteButton(name).ClickAsync();
        await ConfirmDeleteButton.ClickAsync();
    }

    // -------------------------------------------------
    // Ports
    // -------------------------------------------------

    public ILocator PortGroupSection => Ports.Root(_portsPrefix);

    public ILocator PortGroup(int index) => Ports.PortGroup(_portsPrefix, index);

    public ILocator Port(int groupIndex, int portIndex)
        => Ports.Port(_portsPrefix, groupIndex, portIndex);

    public async Task AddPortGroupAsync(string type, string speed, int count)
        => await Ports.AddPortGroupAsync(_portsPrefix, type, speed, count);

    public async Task AssertPortGroupVisibleAsync(int index)
        => await Ports.AssertPortGroupVisibleAsync(_portsPrefix, index);

    public async Task AssertPortVisibleAsync(int groupIndex, int portIndex)
        => await Ports.AssertPortVisibleAsync(_portsPrefix, groupIndex, portIndex);
}
