using System.ComponentModel.DataAnnotations;
using RackPeek.Domain.Api;

namespace RackPeek.Web.Api;

public static class InventoryEndpoints
{
    public static void MapInventoryApi(this WebApplication app)
    {
        app.MapPost("/api/inventory",
                async (ImportYamlRequest request,
                    UpsertInventoryUseCase useCase) =>
                {
                    try
                    {
                        var result = await useCase.ExecuteAsync(request);
                        return Results.Ok(result);
                    }
                    catch (ValidationException ex)
                    {
                        return Results.BadRequest(new { error = ex.Message });
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new { error = $"Import failed: {ex.Message}" });
                    }
                })
            .AddEndpointFilter<ApiKeyEndpointFilter>()
            .DisableAntiforgery();
    }
}