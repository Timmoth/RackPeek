using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using RackPeek.Domain;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Persistence.Yaml;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;
using RackPeek.Web.Components;
using RackPeek.Yaml;
using Shared.Rcl;

namespace RackPeek.Web;

public class Program
{
    public static async Task Main(string[] args)
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

        builder.Services.AddScoped<ITextFileStore, PhysicalTextFileStore>();

        var resources = new ResourceCollection();
        builder.Services.AddSingleton(resources);
        
        builder.Services.AddScoped<IResourceCollection>(sp =>
            new YamlResourceCollection(
                "./config/config.yaml",
                sp.GetRequiredService<ITextFileStore>(),
                sp.GetRequiredService<ResourceCollection>()));
        
        
        // Infrastructure
        builder.Services.AddScoped<IHardwareRepository, YamlHardwareRepository>();
        builder.Services.AddScoped<ISystemRepository, YamlSystemRepository>();
        builder.Services.AddScoped<IServiceRepository, YamlServiceRepository>();
        builder.Services.AddScoped<IResourceRepository, YamlResourceRepository>();
        
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

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        await app.RunAsync();
    }
}