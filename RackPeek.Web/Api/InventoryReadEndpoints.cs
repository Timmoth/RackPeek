using System.ComponentModel.DataAnnotations;
using RackPeek.Domain.Api;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Services.UseCases;
using RackPeek.Domain.Resources.SystemResources.UseCases;
using RackPeek.Domain.Search;
using RackPeek.Mcp.Tools;

namespace RackPeek.Web.Api;

/// <summary>
///     Read-only counterparts to POST /api/inventory, gated by the same API key.
///     The full document endpoint is the workhorse: a client fetches it once and
///     renders lists, details and trees from it. Summary, search and tree mirror the
///     MCP query tools and reuse their response records so the two surfaces cannot drift.
/// </summary>
public static class InventoryReadEndpoints {
    public static void MapInventoryReadApi(this WebApplication app) {
        RouteGroupBuilder group = app.MapGroup("/api/inventory")
            .AddEndpointFilter<ApiKeyEndpointFilter>()
            .DisableAntiforgery();

        group.MapGet("", async (IResourceCollection repo) => {
            try {
                return Results.Json(await InventoryDocumentJson.BuildDocumentAsync(repo),
                    InventoryDocumentJson.Options);
            }
            catch (Exception ex) {
                return ReadError(ex);
            }
        });

        group.MapGet("summary", async (
            IResourceCollection repo,
            GetHardwareUseCaseSummary hardwareSummary,
            GetSystemSummaryUseCase systemsSummary,
            GetServiceSummaryUseCase servicesSummary) => {
                try {
                    var result = new InfrastructureSummary(
                        await hardwareSummary.ExecuteAsync(),
                        await systemsSummary.ExecuteAsync(),
                        await servicesSummary.ExecuteAsync(),
                        await repo.GetTagsAsync(),
                        await repo.GetLabelsAsync());

                    return Results.Ok(result);
                }
                catch (Exception ex) {
                    return ReadError(ex);
                }
            });

        group.MapGet("search", async (string q, int? max, IResourceCollection repo) => {
            try {
                IReadOnlyList<Resource> all = await repo.GetAllOfTypeAsync<Resource>();

                return Results.Ok(new SearchResults(
                    GlobalSearchService.Search(all, q, max ?? 8)));
            }
            catch (Exception ex) {
                return ReadError(ex);
            }
        });

        group.MapGet("tree", async (IHardwareRepository repo) => {
            try {
                return Results.Ok(new TreeResult(await repo.GetTreeAsync()));
            }
            catch (Exception ex) {
                return ReadError(ex);
            }
        });
    }

    private static IResult ReadError(Exception ex) => ex switch {
        NotFoundException => Results.NotFound(new { error = ex.Message }),
        ValidationException => Results.BadRequest(new { error = ex.Message }),
        _ => Results.Problem(ex.Message)
    };
}
