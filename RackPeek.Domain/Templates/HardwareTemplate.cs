using RackPeek.Domain.Resources;

namespace RackPeek.Domain.Templates;

/// <summary>
/// Represents a pre-filled hardware specification that can be used as a starting point
/// when adding a new resource. The <see cref="Spec"/> contains all hardware details
/// (ports, model, features) but uses a placeholder name that gets replaced at creation time.
/// </summary>
/// <param name="Id">Unique identifier in the form <c>{Kind}/{Model}</c>.</param>
/// <param name="Kind">Resource kind (e.g. Switch, Router, Firewall).</param>
/// <param name="Model">Human-readable model name used for display.</param>
/// <param name="Spec">Fully populated resource with placeholder name — deep-cloned at use time.</param>
public sealed record HardwareTemplate(
    string Id,
    string Kind,
    string Model,
    Resource Spec);
