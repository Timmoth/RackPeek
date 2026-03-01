using System.ComponentModel.DataAnnotations;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Templates;

namespace RackPeek.Domain.UseCases;

/// <summary>
/// Marker interface for the template-based add use case, enabling open-generic DI registration.
/// </summary>
public interface IAddResourceFromTemplateUseCase<T> : IResourceUseCase<T>
    where T : Resource
{
    /// <summary>
    /// Creates a new resource pre-filled from a hardware template.
    /// </summary>
    /// <param name="name">Name for the new resource.</param>
    /// <param name="templateId">Template identifier in the form <c>{Kind}/{Model}</c>.</param>
    /// <param name="runsOn">Optional parent resources this resource runs on.</param>
    /// <exception cref="ConflictException">A resource with <paramref name="name"/> already exists.</exception>
    /// <exception cref="NotFoundException">The specified <paramref name="templateId"/> was not found.</exception>
    /// <exception cref="ValidationException">The template kind does not match <typeparamref name="T"/>.</exception>
    Task ExecuteAsync(string name, string templateId, List<string>? runsOn = null);
}

/// <summary>
/// Creates a new resource by deep-cloning a hardware template and assigning the caller-supplied name.
/// </summary>
public class AddResourceFromTemplateUseCase<T>(
    IResourceCollection repo,
    IHardwareTemplateStore templateStore
) : IAddResourceFromTemplateUseCase<T> where T : Resource
{
    /// <inheritdoc />
    public async Task ExecuteAsync(string name, string templateId, List<string>? runsOn = null)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var existing = await repo.GetByNameAsync(name);
        if (existing is not null)
            throw new ConflictException($"Resource '{name}' ({existing.Kind}) already exists.");

        var template = await templateStore.GetByIdAsync(templateId);
        if (template is null)
            throw new NotFoundException($"Template '{templateId}' not found.");

        var expectedKind = Resource.GetKind<T>();
        if (!template.Kind.Equals(expectedKind, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException(
                $"Template '{templateId}' is for {template.Kind}, not {expectedKind}.");

        if (runsOn is not null)
        {
            foreach (var parent in runsOn)
            {
                var normalizedParent = Normalize.HardwareName(parent);
                ThrowIfInvalid.ResourceName(normalizedParent);
                var parentResource = await repo.GetByNameAsync(normalizedParent);
                if (parentResource is null)
                    throw new NotFoundException($"Resource '{normalizedParent}' not found.");

                if (!Resource.CanRunOn<T>(parentResource))
                    throw new InvalidOperationException(
                        $"{expectedKind} cannot run on {parentResource.Kind} '{normalizedParent}'.");
            }
        }

        if (template.Spec is not T typedSpec)
            throw new InvalidOperationException(
                $"Template spec is {template.Spec.GetType().Name}, expected {typeof(T).Name}.");

        var clone = Clone.DeepClone(typedSpec);
        clone.Name = name;
        clone.RunsOn = runsOn ?? new List<string>();

        await repo.AddAsync(clone);
    }
}
