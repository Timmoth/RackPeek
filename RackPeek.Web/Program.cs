using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using RackPeek.Domain;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;
using RackPeek.Web.Components;
using RackPeek.Yaml;

namespace RackPeek.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        StaticWebAssetsLoader.UseStaticWebAssets(
            builder.Environment,
            builder.Configuration
        );

        var yamlDir = "./config";
        var basePath = Directory.GetCurrentDirectory();

        // Resolve yamlDir as relative to basePath
        var yamlPath = Path.IsPathRooted(yamlDir)
            ? yamlDir
            : Path.Combine(basePath, yamlDir);

        if (!Directory.Exists(yamlPath))
            throw new DirectoryNotFoundException(
                $"YAML directory not found: {yamlPath}"
            );
        
        var collection = new YamlResourceCollection(Path.Combine(yamlDir, "config.yaml"));


        // Infrastructure
        builder.Services.AddSingleton<IHardwareRepository>(_ => new YamlHardwareRepository(collection));
        builder.Services.AddSingleton<ISystemRepository>(_ => new YamlSystemRepository(collection));
        builder.Services.AddSingleton<IServiceRepository>(_ => new YamlServiceRepository(collection));
        builder.Services.AddSingleton<IResourceRepository>(_ => new YamlResourceRepository(collection));


        builder.Services.AddUseCases();

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

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}