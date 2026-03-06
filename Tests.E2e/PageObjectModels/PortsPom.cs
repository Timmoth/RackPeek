namespace Tests.E2e.PageObjectModels;

using Microsoft.Playwright;

public class PortsPom(IPage page) {
    public TagsPom Tags => new(page);
    public LabelsPom Labels => new(page);
    public PortsPom Ports => new(page);

    private const string _portsPrefix = "accesspoint-ports";

    // -------------------------------------------------
    // Root
    // -------------------------------------------------

    public ILocator Root(string testIdPrefix)
        => page.GetByTestId($"{testIdPrefix}-port-group-section");

    public ILocator AddButton(string testIdPrefix)
        => page.GetByTestId($"{testIdPrefix}-port-group-add");

    // -------------------------------------------------
    // Port Groups
    // -------------------------------------------------

    public ILocator PortGroup(string testIdPrefix, int index)
        => page.GetByTestId($"{testIdPrefix}-port-group-item-{index}");

    public ILocator EditPortGroupButton(string testIdPrefix, int index)
        => page.GetByTestId($"{testIdPrefix}-port-group-edit-{index}");

    public ILocator PortsContainer(string testIdPrefix, int index)
        => page.GetByTestId($"{testIdPrefix}-port-group-ports-{index}");

    // -------------------------------------------------
    // Individual Ports
    // -------------------------------------------------

    public ILocator Port(string testIdPrefix, int groupIndex, int portIndex)
        => page.GetByTestId($"{testIdPrefix}-port-group-visualizer-{groupIndex}-port-{portIndex}");

    // -------------------------------------------------
    // Port Modal
    // -------------------------------------------------

    public ILocator PortModal(string testIdPrefix)
        => page.GetByTestId($"{testIdPrefix}-port-group-port-modal");

    // -------------------------------------------------
    // Connection Modal
    // -------------------------------------------------

    public ILocator ConnectionModal(string testIdPrefix)
        => page.GetByTestId($"{testIdPrefix}-port-group-connection-modal-container");

    public ILocator ResourceASelect(string testIdPrefix)
        => page.GetByTestId($"{testIdPrefix}-port-group-connection-modal-resource-a");

    public ILocator GroupASelect(string testIdPrefix)
        => page.GetByTestId($"{testIdPrefix}-port-group-connection-modal-group-a");

    public ILocator PortASelect(string testIdPrefix)
        => page.GetByTestId($"{testIdPrefix}-port-group-connection-modal-port-a");

    public ILocator ResourceBSelect(string testIdPrefix)
        => page.GetByTestId($"{testIdPrefix}-port-group-connection-modal-resource-b");

    public ILocator GroupBSelect(string testIdPrefix)
        => page.GetByTestId($"{testIdPrefix}-port-group-connection-modal-group-b");

    public ILocator PortBSelect(string testIdPrefix)
        => page.GetByTestId($"{testIdPrefix}-port-group-connection-modal-port-b");

    public ILocator SubmitConnection(string testIdPrefix)
        => page.GetByTestId($"{testIdPrefix}-port-group-connection-modal-submit");

    // -------------------------------------------------
    // Assertions
    // -------------------------------------------------

    public async Task AssertPortGroupVisibleAsync(string prefix, int index)
        => await Assertions.Expect(PortGroup(prefix, index)).ToBeVisibleAsync();

    public async Task AssertPortVisibleAsync(string prefix, int groupIndex, int portIndex)
        => await Assertions.Expect(Port(prefix, groupIndex, portIndex)).ToBeVisibleAsync();

    // -------------------------------------------------
    // Actions
    // -------------------------------------------------

    public async Task AddPortGroupAsync(string prefix) {
        await AddButton(prefix).ClickAsync();
        await Assertions.Expect(PortModal(prefix)).ToBeVisibleAsync();
    }

    public async Task OpenConnectionFromPortAsync(string prefix, int groupIndex, int portIndex) {
        await Port(prefix, groupIndex, portIndex).ClickAsync();
        await Assertions.Expect(ConnectionModal(prefix)).ToBeVisibleAsync();
    }

    public async Task CreateConnectionAsync(
        string prefix,
        string resourceA,
        string groupA,
        string portA,
        string resourceB,
        string groupB,
        string portB) {
        await ResourceASelect(prefix).SelectOptionAsync(
            new SelectOptionValue { Label = resourceA });

        await GroupASelect(prefix).SelectOptionAsync(
            new SelectOptionValue { Label = groupA });

        await PortASelect(prefix).SelectOptionAsync(
            new SelectOptionValue { Label = portA });

        await ResourceBSelect(prefix).SelectOptionAsync(
            new SelectOptionValue { Label = resourceB });

        await GroupBSelect(prefix).SelectOptionAsync(
            new SelectOptionValue { Label = groupB });

        await PortBSelect(prefix).SelectOptionAsync(
            new SelectOptionValue { Label = portB });

        await SubmitConnection(prefix).ClickAsync();
    }

    // -------------------------------------------------
    // Port Modal Fields
    // -------------------------------------------------

    public ILocator PortTypeSelect(string prefix)
        => page.GetByTestId($"{prefix}-port-group-port-modal-type-input");

    public ILocator PortSpeedSelect(string prefix)
        => page.GetByTestId($"{prefix}-port-group-port-modal-speed-input");

    public ILocator PortCountInput(string prefix)
        => page.GetByTestId($"{prefix}-port-group-port-modal-count-input");
    public ILocator PortSubmit(string prefix)
        => page.GetByTestId($"{prefix}-port-group-port-modal-submit");

    public ILocator PortCancel(string prefix)
        => page.GetByTestId($"{prefix}-port-group-port-modal-cancel");

    public async Task AddPortGroupAsync(
        string prefix,
        string type,
        string speed,
        int count) {
        await AddButton(prefix).ClickAsync();

        await Assertions.Expect(PortModal(prefix)).ToBeVisibleAsync();

        await PortTypeSelect(prefix).SelectOptionAsync(
            new SelectOptionValue { Label = type });

        await PortSpeedSelect(prefix).FillAsync(speed.ToString());

        await PortCountInput(prefix).FillAsync(count.ToString());

        await PortSubmit(prefix).ClickAsync();
    }
}
