using RackPeek.Domain;

namespace RackPeek.Web.Api;

public static class ApiStatusEndpoints {
    public const string ApiVersion = "1";

    public static void MapApiStatus(this WebApplication app) {
        app.MapGet("/api/status", () => Results.Ok(new {
            apiVersion = ApiVersion,
            appVersion = RpkConstants.Version
        }));
    }
}
