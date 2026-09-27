using Microsoft.Playwright;

namespace Tests.E2e.PageObjectModels;

public class UpsCardPom(IPage page) {
    public TagsPom Tags => new(page);
    public LabelsPom Labels => new(page);

    public PortsPom Ports => new(page);

    private const string _portsPrefix = "ups-ports";

    // -------------------------------------------------
    // Notes
    // -------------------------------------------------

    public ILocator NotesViewer
        => page.GetByTestId("ups-notes-viewer-container");

    public ILocator NotesEditor
        => page.GetByTestId("ups-notes-editor-container");

    // -------------------------------------------------
    // Confirm Modal (TestIdPrefix="Ups")
    // -------------------------------------------------

    public ILocator ConfirmDeleteButton
        => page.GetByTestId("Ups-confirm-modal-confirm");

    // -------------------------------------------------
    // Rename Modal (TestIdPrefix="ups-rename")
    // -------------------------------------------------

    public ILocator RenameInput
        => page.GetByTestId("ups-rename-string-value-modal-input");

    public ILocator RenameSubmit
        => page.GetByTestId("ups-rename-string-value-modal-submit");

    // -------------------------------------------------
    // Clone Modal (TestIdPrefix="ups-clone")
    // -------------------------------------------------

    public ILocator CloneInput
        => page.GetByTestId("ups-clone-string-value-modal-input");

    public ILocator CloneSubmit
        => page.GetByTestId("ups-clone-string-value-modal-submit");

    // -------------------------------------------------
    // Root
    // -------------------------------------------------

    private static string Sanitize(string value)
        => value.Replace(" ", "-");

    public ILocator Card(string name)
        => page.GetByTestId($"ups-item-{Sanitize(name)}");

    public ILocator Link(string name)
        => page.GetByTestId($"ups-item-{Sanitize(name)}-link");

    // -------------------------------------------------
    // Action Buttons
    // -------------------------------------------------

    public ILocator EditButton(string name)
        => Card(name).GetByTestId("edit-ups-button");

    public ILocator SaveButton(string name)
        => Card(name).GetByTestId("save-ups-button");

    public ILocator CancelButton(string name)
        => Card(name).GetByTestId("cancel-ups-button");

    public ILocator RenameButton(string name)
        => Card(name).GetByTestId("rename-ups-button");

    public ILocator CloneButton(string name)
        => Card(name).GetByTestId("clone-ups-button");

    public ILocator DeleteButton(string name)
        => Card(name).GetByTestId("delete-ups-button");

    // -------------------------------------------------
    // Edit Inputs
    // -------------------------------------------------

    public ILocator ModelInput(string name)
        => Card(name).GetByTestId("ups-model-input");

    public ILocator CapacityInput(string name)
        => Card(name).GetByTestId("ups-capacity-input");

    // -------------------------------------------------
    // View Values
    // -------------------------------------------------

    public ILocator ModelValue(string name)
        => Card(name).GetByTestId("ups-model-value");

    public ILocator CapacityValue(string name)
        => Card(name).GetByTestId("ups-capacity-value");

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
