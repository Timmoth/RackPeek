using Microsoft.Playwright;

namespace Tests.E2e.PageObjectModels;

public class ServiceCardPom(IPage page)
{
    public TagsPom Tags => new(page);

    // -------------------------------------------------
    // Root
    // -------------------------------------------------

    public ILocator Card(string name)
        => page.GetByTestId($"service-item-{Sanitize(name)}");

    public ILocator Link(string name)
        => page.GetByTestId($"service-item-{Sanitize(name)}-link");

    // -------------------------------------------------
    // Actions
    // -------------------------------------------------

    public ILocator EditButton(string name)
        => Card(name).GetByTestId("edit-service-button");

    public ILocator SaveButton(string name)
        => Card(name).GetByTestId("save-service-button");

    public ILocator CancelButton(string name)
        => Card(name).GetByTestId("cancel-service-button");

    public ILocator RenameButton(string name)
        => Card(name).GetByTestId("rename-service-button");

    public ILocator CloneButton(string name)
        => Card(name).GetByTestId("clone-service-button");

    public ILocator DeleteButton(string name)
        => Card(name).GetByTestId("delete-service-button");

    // -------------------------------------------------
    // Inputs (Edit Mode)
    // -------------------------------------------------

    public ILocator IpInput(string name)
        => Card(name).GetByTestId("service-ip-input");

    public ILocator PortInput(string name)
        => Card(name).GetByTestId("service-port-input");

    public ILocator ProtocolInput(string name)
        => Card(name).GetByTestId("service-protocol-input");

    public ILocator UrlInput(string name)
        => Card(name).GetByTestId("service-url-input");

    public ILocator RunsOnButton(string name)
        => Card(name).GetByTestId("service-runson-button");

    // -------------------------------------------------
    // View Mode Values
    // -------------------------------------------------

    public ILocator IpValue(string name)
        => Card(name).GetByTestId("service-ip-value");

    public ILocator PortValue(string name)
        => Card(name).GetByTestId("service-port-value");

    public ILocator ProtocolValue(string name)
        => Card(name).GetByTestId("service-protocol-value");

    public ILocator UrlValue(string name)
        => Card(name).GetByTestId("service-url-value");

    // -------------------------------------------------
    // Notes
    // -------------------------------------------------

    public ILocator NotesViewer
        => page.GetByTestId("service-notes-viewer-container");

    public ILocator NotesEditor
        => page.GetByTestId("service-notes-editor-container");

    // -------------------------------------------------
    // Confirm Modal
    // -------------------------------------------------

    public ILocator ConfirmDeleteButton
        => page.GetByTestId("service-delete-confirm-modal-confirm");

    // -------------------------------------------------
    // High-Level Actions
    // -------------------------------------------------

    public async Task AssertVisibleAsync(string name)
    {
        await Assertions.Expect(Card(name)).ToBeVisibleAsync();
    }

    public async Task BeginEditAsync(string name)
        => await EditButton(name).ClickAsync();

    public async Task SaveAsync(string name)
        => await SaveButton(name).ClickAsync();

    public async Task CancelAsync(string name)
        => await CancelButton(name).ClickAsync();

    public async Task RenameAsync(string currentName, string newName)
    {
        await RenameButton(currentName).ClickAsync();
        await page.GetByTestId("service-rename-string-value-modal-input").FillAsync(newName);
        await page.GetByTestId("service-rename-string-value-modal-submit").ClickAsync();
        await page.WaitForURLAsync($"**/resources/services/{newName}");
    }

    public async Task CloneAsync(string currentName, string cloneName)
    {
        await CloneButton(currentName).ClickAsync();
        await page.GetByTestId("service-clone-string-value-modal-input").FillAsync(cloneName);
        await page.GetByTestId("service-clone-string-value-modal-submit").ClickAsync();
        await page.WaitForURLAsync($"**/resources/services/{cloneName}");
    }

    public async Task DeleteAsync(string name)
    {
        await DeleteButton(name).ClickAsync();
        await ConfirmDeleteButton.ClickAsync();
    }

    private static string Sanitize(string value)
        => value.Replace(" ", "-");
}
