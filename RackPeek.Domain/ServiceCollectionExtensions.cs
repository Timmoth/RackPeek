using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace RackPeek.Domain;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUseCases(
        this IServiceCollection services)
    {
        var usecases = Assembly.GetAssembly(typeof(IUseCase))
            ?.GetTypes()
            .Where(t =>
                !t.IsAbstract &&
                typeof(IUseCase).IsAssignableFrom(t)
            );

        foreach (var type in usecases) services.AddScoped(type);

        return services;
    }
}