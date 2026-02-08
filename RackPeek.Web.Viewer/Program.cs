using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RackPeek.Domain;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Persistence.Yaml;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;
using RackPeek.Yaml;

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

        builder.Services.AddScoped<IResourceCollection>(sp =>
            new YamlResourceCollection(
                "config.yaml",
                sp.GetRequiredService<ITextFileStore>()));
        
        services.AddScoped<IHardwareRepository, YamlHardwareRepository>();
        services.AddScoped<ISystemRepository, YamlSystemRepository>();
        services.AddScoped<IServiceRepository, YamlServiceRepository>();
        services.AddScoped<IResourceRepository, YamlResourceRepository>();
        
        builder.Services.AddUseCases();
        
        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        await builder.Build().RunAsync();
    }
}