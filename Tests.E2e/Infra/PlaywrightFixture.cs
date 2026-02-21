using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Playwright;

namespace Tests.E2e.Infra;

public class PlaywrightFixture : IAsyncLifetime
{
    // Change this if needed
    private const string DockerImage = "rackpeek:ci";
    private IContainer _container = default!;

    private IPlaywright _playwright = default!;
    public IBrowser Browser { get; private set; } = default!;
    public string BaseUrl { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        _container = new ContainerBuilder(DockerImage)
            .WithPortBinding(8080, true) // random host port
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(r => r
                        .ForPort(8080)
                        .ForPath("/")))
            .Build();

        await _container.StartAsync();

        var mappedPort = _container.GetMappedPublicPort(8080);
        BaseUrl = $"http://127.0.0.1:{mappedPort}";

        Console.WriteLine($"RackPeek running at: {BaseUrl}");

        _playwright = await Playwright.CreateAsync();

        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            //Headless = false,
            SlowMo = 500,
            Args = new[]
            {
                "--disable-dev-shm-usage",
                "--no-sandbox"
            }
        });
        Assertions.SetDefaultExpectTimeout(15000);

    }

    public async Task DisposeAsync()
    {
        if (Browser != null)
            await Browser.DisposeAsync();

        _playwright?.Dispose();

        if (_container != null)
            await _container.DisposeAsync();
    }
}