using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using RackPeek.Domain;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Persistence.Yaml;
using RackPeek.Web.Api;
using RackPeek.Web.Components;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Shared.Rcl;

namespace RackPeek.Web;

public class Program
{
    public static async Task<WebApplication> BuildApp(WebApplicationBuilder builder)
    {
        StaticWebAssetsLoader.UseStaticWebAssets(
            builder.Environment,
            builder.Configuration
        );

        builder.Configuration.AddJsonFile($"appsettings.json", optional: true, reloadOnChange: false);
        
        var yamlDir = builder.Configuration.GetValue<string>("RPK_YAML_DIR") ?? "./config";
        var yamlFileName = "config.yaml";

        var basePath = Directory.GetCurrentDirectory();

        var yamlPath = Path.IsPathRooted(yamlDir)
            ? yamlDir
            : Path.Combine(basePath, yamlDir);

        Directory.CreateDirectory(yamlPath);

        var yamlFilePath = Path.Combine(yamlPath, yamlFileName);

        if (!File.Exists(yamlFilePath))
        {
            // Create empty file safely
            await using var fs = new FileStream(
                yamlFilePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);
            // optionally write default YAML content
            await using var writer = new StreamWriter(fs);
            await writer.WriteLineAsync("# default config");
        }

        builder.Services.AddScoped<ITextFileStore, PhysicalTextFileStore>();
        
        builder.Services.AddScoped(sp =>
        {
            var nav = sp.GetRequiredService<NavigationManager>();
            return new HttpClient
            {
                BaseAddress = new Uri(nav.BaseUri)
            };
        });


        var resources = new ResourceCollection();
        builder.Services.AddSingleton(resources);
        builder.Services.AddScoped<RackPeekConfigMigrationDeserializer>();

        builder.Services.AddScoped<IResourceCollection>(sp =>
            new YamlResourceCollection(
                yamlFilePath,
                sp.GetRequiredService<ITextFileStore>(),
                sp.GetRequiredService<ResourceCollection>(),
                sp.GetRequiredService<RackPeekConfigMigrationDeserializer>()));

        // Infrastructure
        builder.Services.AddYamlRepos();

        builder.Services.AddUseCases();
        builder.Services.AddCommands();
        builder.Services.AddScoped<IConsoleEmulator, ConsoleEmulator>();

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseWhen(
            context => !context.Request.Path.StartsWithSegments("/api"),
            appBuilder => appBuilder.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));
        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseAntiforgery();

        app.MapInventoryApi();

        app.MapStaticAssets();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        return app;
    }

    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);        
        var app = await BuildApp(builder);
        await app.RunAsync();
    }
}
