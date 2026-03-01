using System.ComponentModel.DataAnnotations;
using NSubstitute;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Switches;
using RackPeek.Domain.Resources.Routers;
using RackPeek.Domain.Resources.SubResources;
using RackPeek.Domain.Templates;
using RackPeek.Domain.UseCases;

namespace Tests.Unit.UseCases;

public class AddResourceFromTemplateUseCaseTests
{
    private readonly IResourceCollection _repo = Substitute.For<IResourceCollection>();
    private readonly IHardwareTemplateStore _templateStore = Substitute.For<IHardwareTemplateStore>();

    private HardwareTemplate CreateSwitchTemplate(string model = "TestSwitch-24")
    {
        var spec = new Switch
        {
            Name = model,
            Kind = "Switch",
            Model = model,
            Managed = true,
            Poe = true,
            Ports =
            [
                new Port { Type = "rj45", Speed = 1, Count = 24 }
            ]
        };
        return new HardwareTemplate($"Switch/{model}", "Switch", model, spec);
    }

    [Fact]
    public async Task execute_async__valid_template__creates_resource_with_specs()
    {
        // Arrange
        var template = CreateSwitchTemplate();
        _templateStore.GetByIdAsync("Switch/TestSwitch-24").Returns(template);
        _repo.GetByNameAsync("my-switch").Returns((Resource?)null);
        var sut = new AddResourceFromTemplateUseCase<Switch>(_repo, _templateStore);

        // Act
        await sut.ExecuteAsync("my-switch", "Switch/TestSwitch-24");

        // Assert
        await _repo.Received(1).AddAsync(Arg.Is<Switch>(s =>
            s.Name == "my-switch" &&
            s.Model == "TestSwitch-24" &&
            s.Managed == true &&
            s.Poe == true &&
            s.Ports != null &&
            s.Ports.Count == 1 &&
            s.Ports[0].Type == "rj45" &&
            s.Ports[0].Count == 24));
    }

    [Fact]
    public async Task execute_async__duplicate_name__throws_conflict()
    {
        // Arrange
        _repo.GetByNameAsync("existing").Returns(new Switch { Name = "existing" });
        var sut = new AddResourceFromTemplateUseCase<Switch>(_repo, _templateStore);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(
            () => sut.ExecuteAsync("existing", "Switch/TestSwitch-24"));
    }

    [Fact]
    public async Task execute_async__template_not_found__throws_not_found()
    {
        // Arrange
        _repo.GetByNameAsync("new-switch").Returns((Resource?)null);
        _templateStore.GetByIdAsync("Switch/NonExistent").Returns((HardwareTemplate?)null);
        var sut = new AddResourceFromTemplateUseCase<Switch>(_repo, _templateStore);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => sut.ExecuteAsync("new-switch", "Switch/NonExistent"));
        Assert.Contains("NonExistent", ex.Message);
    }

    [Fact]
    public async Task execute_async__kind_mismatch__throws_validation()
    {
        // Arrange
        var routerTemplate = new HardwareTemplate(
            "Router/SomeRouter",
            "Router",
            "SomeRouter",
            new Router { Name = "SomeRouter", Kind = "Router" });
        _repo.GetByNameAsync("new-switch").Returns((Resource?)null);
        _templateStore.GetByIdAsync("Router/SomeRouter").Returns(routerTemplate);
        var sut = new AddResourceFromTemplateUseCase<Switch>(_repo, _templateStore);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => sut.ExecuteAsync("new-switch", "Router/SomeRouter"));
        Assert.Contains("Router", ex.Message);
        Assert.Contains("Switch", ex.Message);
    }

    [Fact]
    public async Task execute_async__normalizes_name()
    {
        // Arrange
        var template = CreateSwitchTemplate();
        _templateStore.GetByIdAsync("Switch/TestSwitch-24").Returns(template);
        _repo.GetByNameAsync("trimmed-switch").Returns((Resource?)null);
        var sut = new AddResourceFromTemplateUseCase<Switch>(_repo, _templateStore);

        // Act
        await sut.ExecuteAsync("  trimmed-switch  ", "Switch/TestSwitch-24");

        // Assert
        await _repo.Received(1).AddAsync(Arg.Is<Switch>(s =>
            s.Name == "trimmed-switch"));
    }

    [Fact]
    public async Task execute_async__empty_name__throws()
    {
        // Arrange
        var sut = new AddResourceFromTemplateUseCase<Switch>(_repo, _templateStore);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => sut.ExecuteAsync("", "Switch/TestSwitch-24"));
    }

    [Fact]
    public async Task execute_async__sets_empty_runs_on_when_null()
    {
        // Arrange
        var template = CreateSwitchTemplate();
        _templateStore.GetByIdAsync("Switch/TestSwitch-24").Returns(template);
        _repo.GetByNameAsync("my-switch").Returns((Resource?)null);
        var sut = new AddResourceFromTemplateUseCase<Switch>(_repo, _templateStore);

        // Act
        await sut.ExecuteAsync("my-switch", "Switch/TestSwitch-24");

        // Assert
        await _repo.Received(1).AddAsync(Arg.Is<Switch>(s =>
            s.RunsOn != null && s.RunsOn.Count == 0));
    }

    [Fact]
    public async Task execute_async__does_not_mutate_template_spec()
    {
        // Arrange
        var template = CreateSwitchTemplate();
        _templateStore.GetByIdAsync("Switch/TestSwitch-24").Returns(template);
        _repo.GetByNameAsync("my-switch").Returns((Resource?)null);
        var sut = new AddResourceFromTemplateUseCase<Switch>(_repo, _templateStore);

        // Act
        await sut.ExecuteAsync("my-switch", "Switch/TestSwitch-24");

        // Assert — original template spec name is unchanged
        Assert.Equal("TestSwitch-24", template.Spec.Name);
    }
}
