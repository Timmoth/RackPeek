using Microsoft.Playwright;

namespace Tests.E2e.PageObjectModels;

public class HardwareTreePom(IPage page)
{
    // -------------------------------------------------
    // Root & State
    // -------------------------------------------------

    public ILocator PageRoot => page.GetByTestId("hardware-page-root");
    public ILocator PageTitle => page.GetByTestId("hardware-page-title");
    public ILocator SubNav => page.GetByTestId("hardware-subnav");

    public ILocator Loading => page.GetByTestId("hardware-loading");
    public ILocator EmptyState => page.GetByTestId("hardware-empty");
    public ILocator Tree => page.GetByTestId("hardware-tree");

    // -------------------------------------------------
    // Navigation
    // -------------------------------------------------

    public ILocator NavServers => page.GetByTestId("nav-servers");
    public ILocator NavSwitches => page.GetByTestId("nav-switches");
    public ILocator NavFirewalls => page.GetByTestId("nav-firewalls");
    public ILocator NavRouters => page.GetByTestId("nav-routers");
    public ILocator NavAccessPoints => page.GetByTestId("nav-accesspoints");
    public ILocator NavUps => page.GetByTestId("nav-ups");
    public ILocator NavDesktops => page.GetByTestId("nav-desktops");
    public ILocator NavLaptops => page.GetByTestId("nav-laptops");

    public async Task GotoAsync(string baseUrl)
    {
        await page.GotoAsync($"{baseUrl}/hardware/tree");
        await AssertLoadedAsync();
    }

    public async Task AssertLoadedAsync()
    {
        await Assertions.Expect(PageRoot).ToBeVisibleAsync();
        await Assertions.Expect(PageTitle).ToBeVisibleAsync();
    }

    public async Task WaitForTreeAsync()
    {
        await Assertions.Expect(Tree).ToBeVisibleAsync();
    }

    // -------------------------------------------------
    // Hardware Groups
    // -------------------------------------------------

    public ILocator HardwareGroup(string kind)
    {
        return page.GetByTestId($"hardware-group-{kind}");
    }

    public ILocator HardwareGroupTitle(string kind)
    {
        return page.GetByTestId($"hardware-group-title-{kind}");
    }

    public async Task AssertHardwareGroupExists(string kind)
    {
        await Assertions.Expect(HardwareGroup(kind)).ToBeVisibleAsync();
    }

    // -------------------------------------------------
    // Hardware Items
    // -------------------------------------------------

    public ILocator HardwareItem(string hardwareName)
    {
        return page.GetByTestId($"hardware-item-{hardwareName}");
    }

    public ILocator HardwareLink(string hardwareName)
    {
        return page.GetByTestId($"hardware-link-{hardwareName}");
    }

    public ILocator HardwareName(string hardwareName)
    {
        return page.GetByTestId($"hardware-name-{hardwareName}");
    }

    public async Task OpenHardwareAsync(string hardwareName)
    {
        await HardwareLink(hardwareName).ClickAsync();
        await page.WaitForURLAsync($"**/resources/hardware/{hardwareName}");
    }

    // -------------------------------------------------
    // Systems Under Hardware
    // -------------------------------------------------

    public ILocator SystemList(string hardwareName)
    {
        return page.GetByTestId($"system-list-{hardwareName}");
    }

    public ILocator SystemItem(string systemName)
    {
        return page.GetByTestId($"system-item-{systemName}");
    }

    public ILocator SystemLink(string systemName)
    {
        return page.GetByTestId($"system-link-{systemName}");
    }

    public async Task OpenSystemAsync(string systemName)
    {
        await SystemLink(systemName).ClickAsync();
        await page.WaitForURLAsync($"**/resources/systems/{systemName}");
    }

    public async Task AssertSystemExists(string systemName)
    {
        await Assertions.Expect(SystemItem(systemName)).ToBeVisibleAsync();
    }

    // -------------------------------------------------
    // Services Under System
    // -------------------------------------------------

    public ILocator ServiceList(string systemName)
    {
        return page.GetByTestId($"service-list-{systemName}");
    }

    public ILocator ServiceItem(string serviceName)
    {
        return page.GetByTestId($"service-item-{serviceName}");
    }

    public ILocator ServiceLink(string serviceName)
    {
        return page.GetByTestId($"service-link-{serviceName}");
    }

    public async Task OpenServiceAsync(string serviceName)
    {
        await ServiceLink(serviceName).ClickAsync();
        await page.WaitForURLAsync($"**/resources/services/{serviceName}");
    }

    public async Task AssertServiceExists(string serviceName)
    {
        await Assertions.Expect(ServiceItem(serviceName)).ToBeVisibleAsync();
    }

    // -------------------------------------------------
    // Secondary Navigation Actions
    // -------------------------------------------------

    public async Task GotoServersListAsync()
    {
        await NavServers.ClickAsync();
        await page.WaitForURLAsync("**/servers/list");
    }

    public async Task GotoSwitchesListAsync()
    {
        await NavSwitches.ClickAsync();
        await page.WaitForURLAsync("**/switches/list");
    }

    public async Task GotoFirewallsListAsync()
    {
        await NavFirewalls.ClickAsync();
        await page.WaitForURLAsync("**/firewalls/list");
    }

    public async Task GotoRoutersListAsync()
    {
        await NavRouters.ClickAsync();
        await page.WaitForURLAsync("**/routers/list");
    }

    public async Task GotoAccessPointsListAsync()
    {
        await NavAccessPoints.ClickAsync();
        await page.WaitForURLAsync("**/accesspoints/list");
    }

    public async Task GotoUpsListAsync()
    {
        await NavUps.ClickAsync();
        await page.WaitForURLAsync("**/ups/list");
    }

    public async Task GotoDesktopsListAsync()
    {
        await NavDesktops.ClickAsync();
        await page.WaitForURLAsync("**/desktops/list");
    }

    public async Task GotoLaptopsListAsync()
    {
        await NavLaptops.ClickAsync();
        await page.WaitForURLAsync("**/laptops/list");
    }
}