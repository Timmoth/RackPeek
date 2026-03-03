using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using RackPeek.Domain;
using RackPeek.Domain.Git;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Persistence.Yaml;
using RackPeek.Web.Api;
using RackPeek.Web.Components;
using Shared.Rcl;

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

        builder.Services.AddSingleton<IGitService>(new GitService(yamlPath));

        var yamlFilePath = Path.Combine(yamlPath, yamlFileName);

        if (!File.Exists(yamlFilePath)) {
            await using var fs = new FileStream(
                yamlFilePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);

            await using var writer = new StreamWriter(fs);
            await writer.WriteLineAsync("# default config");
        }

        builder.Services.ConfigureHttpJsonOptions(options => {
            options.SerializerOptions.Converters.Add(
                new JsonStringEnumConverter());
        });
        builder.Services.AddScoped<ITextFileStore, PhysicalTextFileStore>();

        builder.Services.AddScoped(sp => {
            NavigationManager nav = sp.GetRequiredService<NavigationManager>();
            return new HttpClient {
                BaseAddress = new Uri(nav.BaseUri)
            };
        });

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
            .AddInteractiveServerRenderMode();

        return app;
    }

    public static async Task Main(string[] args) {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        WebApplication app = await BuildApp(builder);
        await app.RunAsync();
    }
}
