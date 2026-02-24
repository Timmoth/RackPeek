using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RackPeek.Domain;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Persistence.Yaml;
using Shared.Rcl;
using DocMigrator.Yaml;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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
        {
            var nav = sp.GetRequiredService<NavigationManager>();
            return new HttpClient
            {
                BaseAddress = new Uri(nav.BaseUri)
            };
        });


        builder.Services.AddScoped<ITextFileStore, WasmTextFileStore>();

        var resources = new ResourceCollection();
        builder.Services.AddSingleton(resources);

        builder.Services.AddSingleton<RackPeekConfigMigrationDeserializer>(sp => 
        {
            var logger = sp.GetRequiredService<ILogger<YamlMigrationDeserializer<YamlRoot>>>();

            // TODO: Add options
            var deserializerBuilder = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties();

            var serializerBuilder = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance);

            return new RackPeekConfigMigrationDeserializer(
                    sp,
                    logger,
                    deserializerBuilder,
                    serializerBuilder);
        });

        var yamlDir = builder.Configuration.GetValue<string>("RPK_YAML_DIR") ?? "config";
        var yamlFilePath = $"{yamlDir}/config.yaml";

        builder.Services.AddScoped<IResourceCollection>(sp =>
            new YamlResourceCollection(
                yamlFilePath,
                sp.GetRequiredService<ITextFileStore>(),
                sp.GetRequiredService<ResourceCollection>(),
                sp.GetRequiredService<RackPeekConfigMigrationDeserializer>()));

        builder.Services.AddYamlRepos();
        builder.Services.AddCommands();
        builder.Services.AddScoped<IConsoleEmulator, ConsoleEmulator>();

        builder.Services.AddUseCases();

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        await builder.Build().RunAsync();
    }
}
