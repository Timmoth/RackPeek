using System.ComponentModel.DataAnnotations;
using ModelContextProtocol;
using RackPeek.Domain.Helpers;

namespace RackPeek.Mcp;

/// <summary>
///     Runs a tool body and rethrows the domain's user-facing exceptions as
///     <see cref="McpException" /> so their message reaches the calling agent as a
///     tool error it can act on. Anything else falls through: the SDK reports those
///     as a generic error, deliberately, so nothing internal leaks over the wire.
/// </summary>
internal static class ToolErrors {
    public static async Task<T> RunAsync<T>(Func<Task<T>> action) {
        try {
            return await action();
        }
        catch (ValidationException ex) {
            throw new McpException($"Invalid input: {ex.Message}");
        }
        catch (NotFoundException ex) {
            throw new McpException(ex.Message);
        }
        catch (ConflictException ex) {
            throw new McpException(ex.Message);
        }
        catch (InvalidOperationException ex) {
            // The connection use cases signal user-correctable mistakes with this
            // ("cannot connect a port to itself", "resource has no ports").
            throw new McpException(ex.Message);
        }
    }
}
