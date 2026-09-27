using Microsoft.Playwright;
using Tests.E2e.Infra;
using Xunit.Abstractions;

namespace Tests.E2e;

/// <summary>
///     Repro for an unfiled bug found during issue triage: the /yaml editor's
///     YamlFileComponent carries its own copy of the kind discriminator map
///     (YamlFileComponent.razor:212-224), and `Other` was never added to it —
///     the fourth hand-maintained kind registry to drift. Saving any config
///     that contains a `kind: Other` resource through the /yaml page fails
///     validation, even though the loader, importer, CLI and MCP all accept
///     it. This test fails on staging until the map (ideally all four
///     registries) is unified.
/// </summary>
public class YamlEditorOtherKindTests(
    PlaywrightFixture fixture,
    ITestOutputHelper output) : E2ETestBase(fixture, output) {
    private readonly PlaywrightFixture _fixture = fixture;
    private readonly ITestOutputHelper _output = output;

    private static string ConfigWithOther(string name) =>
        $"""
         version: 4
         resources:
         - kind: Other
           name: {name}
           model: KVM-4P
           description: Rack KVM switch
         connections: []
         """;

    [Fact]
    public async Task The_Yaml_Editor_Accepts_A_Config_Containing_An_Other_Resource() {
        (IBrowserContext context, IPage page) = await CreatePageAsync();

        var name = $"e2e-oth-{Guid.NewGuid():N}"[..14];

        try {
            await page.GotoAsync($"{_fixture.BaseUrl}/yaml");

            ILocator content = page.GetByTestId("yaml-file-content");
            await Assertions.Expect(content).ToBeVisibleAsync();

            await Assertions.Expect(page.GetByTestId("circuit-probe"))
                .ToHaveAttributeAsync("data-circuit-ready", "true");

            await page.GetByRole(AriaRole.Button, new() { Name = "Edit" }).ClickAsync();

            ILocator textarea = page.Locator("textarea");
            await Assertions.Expect(textarea).ToBeVisibleAsync();
            await textarea.FillAsync(ConfigWithOther(name));

            await page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

            // The same document loads fine through the CLI, the importer and
            // the MCP server; the editor must not reject it. Today it shows
            // a validation error because its discriminator map lacks Other.
            await Assertions.Expect(page.GetByTestId("yaml-file-error")).ToHaveCountAsync(0);
            await Assertions.Expect(content).ToBeVisibleAsync();
            await Assertions.Expect(content).ToContainTextAsync(name);
        }
        catch (Exception) {
            _output.WriteLine($"TEST FAILED — URL: {page.Url}");
            _output.WriteLine(await page.ContentAsync());
            throw;
        }
        finally {
            await context.CloseAsync();
        }
    }
}
