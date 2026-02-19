using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Desktops;
using RackPeek.Domain.Resources.Models;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;
using RackPeek.Domain.UseCases;
using RackPeek.Domain.UseCases.Cpus;
using RackPeek.Domain.UseCases.Drives;
using RackPeek.Domain.UseCases.Tags;

namespace RackPeek.Domain;

public interface IResourceUseCase<T> where T : Resource
{
    
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResourceUseCases(
        this IServiceCollection services,
        Assembly assembly)
    {
        var types = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface);

        foreach (var type in types)
        {
            var resourceUseCaseInterfaces = type.GetInterfaces()
                .Where(i =>
                    i.IsGenericType &&
                    i.GetInterfaces().Any(parent =>
                        parent.IsGenericType &&
                        parent.GetGenericTypeDefinition() == typeof(IResourceUseCase<>)));

            foreach (var serviceType in resourceUseCaseInterfaces)
            {
                services.AddScoped(serviceType, type);
            }
        }

        return services;
    }

    
    public static IServiceCollection AddUseCases(
        this IServiceCollection services)
    {
        services.AddScoped(typeof(IAddResourceUseCase<>), typeof(AddResourceUseCase<>));
        services.AddScoped(typeof(IAddTagUseCase<>), typeof(AddTagUseCase<>));
        services.AddScoped(typeof(ICloneResourceUseCase<>), typeof(CloneResourceUseCase<>));
        services.AddScoped(typeof(IDeleteResourceUseCase<>), typeof(DeleteResourceUseCase<>));
        services.AddScoped(typeof(IRemoveTagUseCase<>), typeof(RemoveTagUseCase<>));
        services.AddScoped(typeof(IGetAllResourcesByKindUseCase<>), typeof(GetAllResourcesByKindUseCase<>));
        services.AddScoped(typeof(IGetResourceByNameUseCase<>), typeof(GetResourceByNameUseCase<>));
        services.AddScoped(typeof(IRenameResourceUseCase<>), typeof(RenameResourceUseCase<>));

        services.AddScoped(typeof(IAddCpuUseCase<>), typeof(AddCpuUseCase<>));
        services.AddScoped(typeof(IRemoveCpuUseCase<>), typeof(RemoveCpuUseCase<>));
        services.AddScoped(typeof(IUpdateCpuUseCase<>), typeof(UpdateCpuUseCase<>));

        
        services.AddScoped(typeof(IAddDriveUseCase<>), typeof(AddDriveUseCase<>));
        services.AddScoped(typeof(IRemoveDriveUseCase<>), typeof(RemoveDriveUseCase<>));
        services.AddScoped(typeof(IUpdateDriveUseCase<>), typeof(UpdateDriveUseCase<>));

        
        var usecases = Assembly.GetAssembly(typeof(IUseCase))
            ?.GetTypes()
            .Where(t =>
                !t.IsAbstract &&
                typeof(IUseCase).IsAssignableFrom(t)
            );

        foreach (var type in usecases) services.AddScoped(type);

        return services;
    }
    
    public static IServiceCollection AddYamlRepos(
        this IServiceCollection services)
    {
        services.AddScoped<IHardwareRepository, YamlHardwareRepository>();
        services.AddScoped<ISystemRepository, YamlSystemRepository>();
        services.AddScoped<IServiceRepository, YamlServiceRepository>();
        return services;
    }
    
    
}