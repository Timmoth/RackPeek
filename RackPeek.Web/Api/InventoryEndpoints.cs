using System.ComponentModel.DataAnnotations;
using RackPeek.Domain.Api;

namespace RackPeek.Web.Api;

public static class InventoryEndpoints
{
    public static void MapInventoryApi(this WebApplication app)
    {
        app.MapPost("/api/inventory", async (HttpRequest httpRequest, UpsertInventoryUseCase useCase) =>
        {
            try
            {
                var request = await httpRequest.ReadFromJsonAsync<InventoryRequest>();
                if (request == null)
                    return Results.BadRequest(new { error = "Invalid request body." });

                var result = await useCase.ExecuteAsync(request);
                return Results.Ok(result);
            }
            catch (ValidationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .AddEndpointFilter<ApiKeyEndpointFilter>()
        .DisableAntiforgery();
    }
}
