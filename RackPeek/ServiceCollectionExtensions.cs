using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using RackPeek.Domain.Diagram;
using RackPeek.Domain.Diagram.UseCases;

namespace RackPeek;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCommands(this IServiceCollection services)
    {
        var commandBaseType = typeof(AsyncCommand);

        var commandTypes = Assembly.GetAssembly(typeof(Program))
            ?.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } && IsAsyncCommand(t));

        foreach (var type in commandTypes) services.AddScoped(type);

        return services;
    }

    private static bool IsAsyncCommand(Type type)
    {
        while (type != null)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(AsyncCommand<>)) return true;

            if (type == typeof(AsyncCommand)) return true;

            type = type.BaseType!;
        }

        return false;
    }
}