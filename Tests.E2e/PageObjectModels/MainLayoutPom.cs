using Microsoft.Playwright;

namespace Tests.E2e.PageObjectModels;

public class MainLayoutPom(IPage page)
{
    public ILocator AppRoot => page.GetByTestId("app-root");
    public ILocator Header => page.GetByTestId("app-header");
    public ILocator PageContent => page.GetByTestId("page-content");

    public ILocator BrandLink => page.GetByTestId("brand-link");
    public ILocator BrandText => page.GetByTestId("brand-text");

    public ILocator NavHome => page.GetByTestId("nav-home");
    public ILocator NavCli => page.GetByTestId("nav-cli");
    public ILocator NavYaml => page.GetByTestId("nav-yaml");
    public ILocator NavHardware => page.GetByTestId("nav-hardware");
    public ILocator NavSystems => page.GetByTestId("nav-systems");
    public ILocator NavServices => page.GetByTestId("nav-services");
    public ILocator NavDocs => page.GetByTestId("nav-docs");


    public async Task GotoHomeAsync()
    {
        await NavHome.ClickAsync();
        await Assertions.Expect(PageContent).ToBeVisibleAsync();
    }

    public async Task GotoHardwareAsync()
    {
        await NavHardware.ClickAsync();
        await page.WaitForURLAsync("**/hardware/**");
    }

    public async Task GotoSystemsAsync()
    {
        await NavSystems.ClickAsync();
        await page.WaitForURLAsync("**/systems/**");
    }

    public async Task GotoServicesAsync()
    {
        await NavServices.ClickAsync();
        await page.WaitForURLAsync("**/services/**");
    }

    public async Task GotoCliAsync()
    {
        await NavCli.ClickAsync();
        await page.WaitForURLAsync("**/cli");
    }

    public async Task GotoYamlAsync()
    {
        await NavYaml.ClickAsync();
        await page.WaitForURLAsync("**/yaml");
    }

    public async Task GotoDocsAsync()
    {
        await NavDocs.ClickAsync();
        await page.WaitForURLAsync("**/docs/**");
    }

    public async Task AssertLoadedAsync()
    {
        await Assertions.Expect(AppRoot).ToBeVisibleAsync();
        await Assertions.Expect(Header).ToBeVisibleAsync();
        await Assertions.Expect(PageContent).ToBeVisibleAsync();
    }
}