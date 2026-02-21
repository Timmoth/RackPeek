using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Playwright;

namespace Tests.E2e.Infra;

public class PlaywrightFixture : IAsyncLifetime
{
    // Change this if needed
    private const string DockerImage = "rackpeek:ci";
    private string _configDirectory = default!;
    private IContainer _container = default!;

    private IPlaywright _playwright = default!;
    public IBrowser Browser { get; private set; } = default!;
    public string BaseUrl { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        // Create isolated config directory per test run
        _configDirectory = Path.Combine(
            Path.GetTempPath(),
            "rackpeek-e2e",
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(_configDirectory);

        File.WriteAllText(
            Path.Combine(_configDirectory, "config.yaml"),
            "# E2E test config");

        _container = new ContainerBuilder()
            .WithImage(DockerImage)
            .WithPortBinding(8080, true) // random host port
            .WithBindMount(_configDirectory, "/app/config")
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
            SlowMo = 200
        });
    }

    public async Task DisposeAsync()
    {
        if (Browser != null)
            await Browser.DisposeAsync();

        _playwright?.Dispose();

        if (_container != null)
            await _container.DisposeAsync();

        if (!string.IsNullOrWhiteSpace(_configDirectory) &&
            Directory.Exists(_configDirectory))
            Directory.Delete(_configDirectory, true);
    }
}