using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace RackPeek;

public sealed class TypeRegistrar(IServiceCollection services) : ITypeRegistrar
{
    public void Register(Type service, Type implementation)
    {
        services.AddSingleton(service, implementation);
    }

    public void RegisterInstance(Type service, object implementation)
    {
        services.AddSingleton(service, implementation);
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        services.AddSingleton(service, _ => factory());
    }

    public ITypeResolver Build()
    {
        return new TypeResolver(services.BuildServiceProvider());
    }
}

public sealed class TypeResolver(ServiceProvider provider) : ITypeResolver, IDisposable
{
    public object? Resolve(Type? type)
    {
        return type == null ? null : provider.GetService(type);
    }

    public void Dispose()
    {
        provider.Dispose();
    }
}