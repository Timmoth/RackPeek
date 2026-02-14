using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Models;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;
using RackPeek.Domain.UseCases;
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
        services.AddScoped(typeof(IAddTagUseCase<>), typeof(AddTagUseCase<>));
        services.AddScoped(typeof(IRemoveTagUseCase<>), typeof(RemoveTagUseCase<>));
        services.AddScoped(typeof(IAddResourceUseCase<>), typeof(AddResourceUseCase<>));
        
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
        services.AddScoped<IResourceRepository, YamlResourceRepository>();
        
        services.AddScoped<IResourceRepo<AccessPoint>, YamlHardwareRepo<AccessPoint>>();
        services.AddScoped<IResourceRepo<Desktop>, YamlHardwareRepo<Desktop>>();
        services.AddScoped<IResourceRepo<Firewall>, YamlHardwareRepo<Firewall>>();
        services.AddScoped<IResourceRepo<Laptop>, YamlHardwareRepo<Laptop>>();
        services.AddScoped<IResourceRepo<Router>, YamlHardwareRepo<Router>>();
        services.AddScoped<IResourceRepo<Server>, YamlHardwareRepo<Server>>();
        services.AddScoped<IResourceRepo<Switch>, YamlHardwareRepo<Switch>>();
        services.AddScoped<IResourceRepo<Ups>, YamlHardwareRepo<Ups>>();
        
        services.AddScoped<IResourceRepo<SystemResource>, YamlSystemRepository>();
        services.AddScoped<IResourceRepo<Service>, YamlServiceRepository>();
        


        return services;
    }
    
    
}