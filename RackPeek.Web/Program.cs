using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using RackPeek.Domain;
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
        
        var collection = new YamlResourceCollection();
        var basePath = Directory.GetCurrentDirectory();

        // Resolve yamlDir as relative to basePath
        var yamlPath = Path.IsPathRooted(yamlDir)
            ? yamlDir
            : Path.Combine(basePath, yamlDir);

        if (!Directory.Exists(yamlPath))
            throw new DirectoryNotFoundException(
                $"YAML directory not found: {yamlPath}"
            );

        // Load all .yml and .yaml files
        var yamlFiles = Directory.EnumerateFiles(yamlPath, "*.yml")
            .Concat(Directory.EnumerateFiles(yamlPath, "*.yaml"))
            .ToArray();
        
        collection.LoadFiles(yamlFiles.Select(f => Path.Combine(basePath, f)));

        // Infrastructure
        builder.Services.AddScoped<IHardwareRepository>(_ => new YamlHardwareRepository(collection));
        builder.Services.AddScoped<ISystemRepository>(_ => new YamlSystemRepository(collection));
        builder.Services.AddScoped<IServiceRepository>(_ => new YamlServiceRepository(collection));

        
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