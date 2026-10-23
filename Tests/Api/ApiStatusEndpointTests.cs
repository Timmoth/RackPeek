using System.Net;
using System.Text.Json;
using RackPeek.Domain;
using RackPeek.Web.Api;
using Xunit.Abstractions;

namespace Tests.Api;

[Collection("Process-wide static state")]
public class ApiStatusEndpointTests(ITestOutputHelper output) : ApiTestBase(output) {
    [Fact]
    public async Task Status_Returns_Api_And_App_Versions_Without_An_Api_Key() {
        HttpResponseMessage response = await CreateClient().GetAsync("/api/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(ApiStatusEndpoints.ApiVersion,
            document.RootElement.GetProperty("apiVersion").GetString());
        Assert.Equal(RpkConstants.Version,
            document.RootElement.GetProperty("appVersion").GetString());
    }
}
