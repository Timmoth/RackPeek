namespace RackPeek.Domain.Helpers;

/// <summary>
///     The config file exists but cannot be read as a RackPeek document — damaged,
///     truncated, or not YAML. Distinct from an unreadable store (IO errors), which is
///     tolerated at boot: a damaged file must fail loudly on every read and write so a
///     partial or empty inventory is never served, and never persisted over the
///     user's file (#337).
/// </summary>
public sealed class ConfigLoadException : Exception {
    public ConfigLoadException(string message)
        : base(message) {
    }

    public ConfigLoadException(string message, Exception innerException)
        : base(message, innerException) {
    }
}
