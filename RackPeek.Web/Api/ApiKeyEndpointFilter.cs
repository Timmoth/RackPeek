using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace RackPeek.Web.Api;

public class ApiKeyEndpointFilter(IConfiguration configuration) : IEndpointFilter {
    private const string _apiKeyHeaderName = "X-Api-Key";

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next) {
        var expectedKey = configuration["RPK_API_KEY"];

        if (string.IsNullOrWhiteSpace(expectedKey))
            return Results.Json(new { error = "API key not configured on server" }, statusCode: 503);

        if (!context.HttpContext.Request.Headers.TryGetValue(_apiKeyHeaderName, out StringValues providedKey)
            || !SecureEquals(providedKey.ToString(), expectedKey))
            return Results.Json(new { error = "Unauthorized" }, statusCode: 401);

        return await next(context);
    }

    private static bool SecureEquals(string a, string b) {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);

        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}
