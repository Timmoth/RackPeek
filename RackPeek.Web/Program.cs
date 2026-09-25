using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using RackPeek.Domain;
using RackPeek.Domain.Git;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Persistence.Yaml;
using RackPeek.Web.Api;
using RackPeek.Web.Components;
using Shared.Rcl;
using Shared.Rcl.Docs;
using Shared.Rcl.Servers;

namespace RackPeek.Web;

public class Program {
    public static async Task<WebApplication> BuildApp(WebApplicationBuilder builder) {
        StaticWebAssetsLoader.UseStaticWebAssets(
            builder.Environment,
            builder.Configuration
        );

        var yamlDir = builder.Configuration.GetValue<string>("RPK_YAML_DIR") ?? "./config";
        var yamlFileName = "config.yaml";

        var basePath = Directory.GetCurrentDirectory();
        var yamlPath = Path.IsPathRooted(yamlDir)
            ? yamlDir
            : Path.Combine(basePath, yamlDir);

        Directory.CreateDirectory(yamlPath);

        var yamlFilePath = Path.Combine(yamlPath, yamlFileName);

        if (!File.Exists(yamlFilePath)) {
            try {
                await using var fs = new FileStream(
                    yamlFilePath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None);

                await using var writer = new StreamWriter(fs);
                await writer.WriteLineAsync("# default config");
            }
            catch (IOException) when (File.Exists(yamlFilePath)) {
                // Another instance created the file between the existence
                // check and CreateNew — the config is there, carry on.
            }
        }

        // Persist DataProtection keys next to the config so they live on the
        // mounted volume: they survive container recreation, and key writes
        // no longer depend on a writable user profile or /tmp — both of
        // which are unavailable in hardened Docker setups (#312).
        var keysPath = Path.Combine(yamlPath, ".dataprotection");
        Directory.CreateDirectory(keysPath);
        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
            .SetApplicationName("RackPeek");

        builder.Services.ConfigureHttpJsonOptions(options => {
            options.SerializerOptions.Converters.Add(
                new JsonStringEnumConverter());
        });
        builder.Services.AddScoped<ITextFileStore, PhysicalTextFileStore>();

        builder.Services.AddScoped<IDocsContentProvider, StaticWebAssetDocsContentProvider>();

        builder.Services.AddGitServices(builder.Configuration, yamlPath);

        var resources = new ResourceCollection();
        builder.Services.AddSingleton(resources);
        builder.Services.AddScoped<RackPeekConfigMigrationDeserializer>();
        builder.Services.AddScoped<IResourceYamlMigrationService, ResourceYamlMigrationService>();

        builder.Services.AddScoped<IResourceCollection>(sp =>
            new YamlResourceCollection(
                yamlFilePath,
                sp.GetRequiredService<ITextFileStore>(),
                sp.GetRequiredService<ResourceCollection>(),
                sp.GetRequiredService<IResourceYamlMigrationService>()));

        // Infrastructure
        builder.Services.AddYamlRepos();
        builder.Services.AddUseCases();
        builder.Services.AddCommands();
        builder.Services.AddScoped<IConsoleEmulator, ConsoleEmulator>();

        // Razor Components
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        WebApplication app = builder.Build();

        // Read the config into memory before anything can be served. Blazor reloads it
        // on every circuit init, but the inventory API has no circuit — without this it
        // would merge against an empty collection and persist that over the user's file,
        // destroying the inventory on the first request after a restart.
        await using (AsyncServiceScope scope = app.Services.CreateAsyncScope()) {
            try {
                await scope.ServiceProvider.GetRequiredService<IResourceCollection>().LoadAsync();
            }
            catch (Exception ex) {
                // An unreadable config must not stop the server booting: the web UI is
                // how someone fixes it, and a container that will not start is worse
                // than one showing the error. Blazor surfaces it on the first page load,
                // and every write path re-checks the load before persisting anything,
                // so booting in this state cannot overwrite the file.
                scope.ServiceProvider.GetRequiredService<ILogger<Program>>()
                    .LogError(ex, "Could not read the config at {Path}. Fix it in the web UI.", yamlFilePath);
            }
        }

        if (!app.Environment.IsDevelopment()) {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseAntiforgery();

        app.MapInventoryApi();

        app.MapStaticAssets();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddAdditionalAssemblies(typeof(ServersListPage).Assembly);

        return app;
    }

    public static async Task Main(string[] args) {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        WebApplication app = await BuildApp(builder);
        await app.RunAsync();
    }
}
