using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;

namespace Tests.HardwareResources;

public class UsecaseTestHost
{
    private readonly ServiceCollection _sc;

    public UsecaseTestHost()
    {
        HardwareRepo = Substitute.For<IHardwareRepository>();
        SystemRepo = Substitute.For<ISystemRepository>();
        ServiceRepo = Substitute.For<IServiceRepository>();
        ResourceRepo = Substitute.For<IResourceRepository>();
        
        _sc = new ServiceCollection();
        _sc.AddSingleton<IHardwareRepository>(HardwareRepo);
        _sc.AddSingleton<ISystemRepository>(SystemRepo);
        _sc.AddSingleton<IServiceRepository>(ServiceRepo);
        _sc.AddSingleton<IResourceRepository>(ResourceRepo);
    }

    public IHardwareRepository HardwareRepo { get; set; }
    public ISystemRepository SystemRepo { get; set; }
    public IServiceRepository ServiceRepo { get; set; }
    
    public IResourceRepository ResourceRepo { get; set; }
    public T Get<T>() where T : notnull
    {
        _sc.AddSingleton(typeof(T));
        var sp = _sc.BuildServiceProvider();
        return sp.GetRequiredService<T>();
    }
}