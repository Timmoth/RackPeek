using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RackPeek.Domain;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Persistence.Yaml;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Web.Viewer;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        var services = builder.Services;
        builder.Services.AddScoped(sp =>
            new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        builder.Services.AddScoped<ITextFileStore, WasmTextFileStore>();

        var resources = new ResourceCollection();
        builder.Services.AddSingleton(resources);

        var yamlDir = builder.Configuration.GetValue<string>("RPK_YAML_DIR") ?? "config";
        var yamlFilePath = $"{yamlDir}/config.yaml";

        builder.Services.AddScoped<IResourceCollection>(sp =>
            new YamlResourceCollection(
                yamlFilePath,
                sp.GetRequiredService<ITextFileStore>(),
                sp.GetRequiredService<ResourceCollection>()));


        services.AddScoped<IHardwareRepository, YamlHardwareRepository>();
        services.AddScoped<ISystemRepository, YamlSystemRepository>();
        services.AddScoped<IServiceRepository, YamlServiceRepository>();
        services.AddScoped<IResourceRepository, YamlResourceRepository>();

        builder.Services.AddCommands();
        builder.Services.AddScoped<IConsoleEmulator, ConsoleEmulator>();

        builder.Services.AddUseCases();

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        await builder.Build().RunAsync();
    }
}