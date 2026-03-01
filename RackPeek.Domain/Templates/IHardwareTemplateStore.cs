namespace RackPeek.Domain.Templates;

/// <summary>
/// Read-only store of known hardware templates that can be used to pre-fill
/// resource specifications when adding new resources.
/// </summary>
public interface IHardwareTemplateStore
{
    /// <summary>
    /// Returns all templates matching the specified resource kind (case-insensitive).
    /// </summary>
    /// <param name="kind">Resource kind such as "Switch", "Router", or "Firewall".</param>
    Task<IReadOnlyList<HardwareTemplate>> GetAllByKindAsync(string kind);

    /// <summary>
    /// Returns a single template by its identifier, or <c>null</c> if not found.
    /// </summary>
    /// <param name="templateId">Template identifier in the form <c>{Kind}/{Model}</c>.</param>
    Task<HardwareTemplate?> GetByIdAsync(string templateId);

    /// <summary>
    /// Returns all available templates across all resource kinds.
    /// </summary>
    Task<IReadOnlyList<HardwareTemplate>> GetAllAsync();
}
