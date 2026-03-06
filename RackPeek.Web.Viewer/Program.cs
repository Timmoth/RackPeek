using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RackPeek.Domain;
using RackPeek.Domain.Git;
using RackPeek.Domain.Git.UseCases;
using RackPeek.Domain.Git.Queries;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Persistence.Yaml;
using Shared.Rcl;

namespace RackPeek.Web.Viewer;

public class Program {
    public static async Task Main(string[] args) {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        IServiceCollection services = builder.Services;


        builder.Services.AddScoped(sp => {
            NavigationManager nav = sp.GetRequiredService<NavigationManager>();
            return new HttpClient {
                BaseAddress = new Uri(nav.BaseUri)
            };
        });


        builder.Services.AddSingleton<IGitRepository>(new NullGitRepository());
        builder.Services.AddSingleton<IInitRepoUseCase, InitRepoUseCase>();
        builder.Services.AddSingleton<ICommitAllUseCase, CommitAllUseCase>();
        builder.Services.AddSingleton<IRestoreAllUseCase, RestoreAllUseCase>();
        builder.Services.AddSingleton<IPushUseCase, PushUseCase>();
        builder.Services.AddSingleton<IPullUseCase, PullUseCase>();
        builder.Services.AddSingleton<IAddRemoteUseCase, AddRemoteUseCase>();
        builder.Services.AddSingleton<IGetStatusQuery, GetStatusQuery>();
        builder.Services.AddSingleton<IGetBranchQuery, GetBranchQuery>();
        builder.Services.AddSingleton<IGetDiffQuery, GetDiffQuery>();
        builder.Services.AddSingleton<IGetChangedFilesQuery, GetChangedFilesQuery>();
        builder.Services.AddSingleton<IGetLogQuery, GetLogQuery>();
        builder.Services.AddSingleton<IGetSyncStatusQuery, GetSyncStatusQuery>();
        builder.Services.AddScoped<ITextFileStore, WasmTextFileStore>();

        var resources = new ResourceCollection();
        builder.Services.AddSingleton(resources);

        var yamlDir = builder.Configuration.GetValue<string>("RPK_YAML_DIR") ?? "config";
        var yamlFilePath = $"{yamlDir}/config.yaml";
        builder.Services.AddScoped<RackPeekConfigMigrationDeserializer>();
        builder.Services.AddScoped<IResourceYamlMigrationService, ResourceYamlMigrationService>();

        builder.Services.AddScoped<IResourceCollection>(sp =>
            new YamlResourceCollection(
                yamlFilePath,
                sp.GetRequiredService<ITextFileStore>(),
                sp.GetRequiredService<ResourceCollection>(),
                sp.GetRequiredService<IResourceYamlMigrationService>()));

        builder.Services.AddYamlRepos();
        builder.Services.AddCommands();
        builder.Services.AddScoped<IConsoleEmulator, ConsoleEmulator>();

        builder.Services.AddUseCases();

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        await builder.Build().RunAsync();
    }
}
