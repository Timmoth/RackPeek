using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RackPeek.Web;
using Shared.Rcl;
using Xunit.Abstractions;

namespace Tests.Api;

public abstract class ApiTestBase : IDisposable {
    private readonly string _tempDir;
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly ITestOutputHelper Output;

    protected ApiTestBase(ITestOutputHelper output) {
        Output = output;

        _tempDir = Path.Combine(
            Path.GetTempPath(),
            "rackpeek-tests",
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(_tempDir);

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => {
                builder.ConfigureAppConfiguration((context, configBuilder) => {
                    var baseConfig = new Dictionary<string, string?> {
                        ["RPK_API_KEY"] = "test-key-123"
                    };

                    ConfigureTestConfiguration(baseConfig);

                    configBuilder.AddInMemoryCollection(baseConfig);

                    IConfigurationRoot configuration = configBuilder.Build();

                    CliBootstrap.RegisterInternals(
                            new ServiceCollection(),
                            configuration,
                            _tempDir,
                            "test.yaml")
                        .GetAwaiter()
                        .GetResult();
                });

                builder.ConfigureServices(services => {
                    services.AddLogging(logging => {
                        logging.ClearProviders();
                        logging.AddProvider(
                            new XUnitLoggerProvider(Output));
                    });

                    ConfigureTestServices(services);
                });
            });
    }

    public void Dispose() {
        try {
            Factory.Dispose();

            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch {
            // ignore cleanup issues
        }
    }

    /// <summary>
    ///     Override to modify configuration per test class
    /// </summary>
    protected virtual void ConfigureTestConfiguration(
        IDictionary<string, string?> config) {
    }

    /// <summary>
    ///     Override to modify services per test class
    /// </summary>
    protected virtual void ConfigureTestServices(
        IServiceCollection services) {
    }

    protected HttpClient CreateClient(bool withApiKey = false) {
        HttpClient client = Factory.CreateClient();

        if (withApiKey) client.DefaultRequestHeaders.Add("X-Api-Key", "test-key-123");

        return client;
    }
}
