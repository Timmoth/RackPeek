namespace RackPeek.Web.Api;

public class ApiKeyEndpointFilter(IConfiguration configuration) : IEndpointFilter
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var expectedKey = configuration["RPK_API_KEY"];

        if (string.IsNullOrWhiteSpace(expectedKey))
            return Results.StatusCode(503);

        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedKey)
            || providedKey.ToString() != expectedKey)
        {
            return Results.Json(new { error = "Unauthorized" }, statusCode: 401);
        }

        return await next(context);
    }
}
