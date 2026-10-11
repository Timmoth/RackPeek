using System.Net;
using System.Net.Http.Json;
using Xunit.Abstractions;

namespace Tests.Api;

[Collection("Process-wide static state")]
public class InventoryEndpointUnconfiguredTests(ITestOutputHelper output) : ApiTestBase(output) {
    protected override void ConfigureTestConfiguration(IDictionary<string, string?> config) =>
        config.Remove("RPK_API_KEY");

    [Fact]
    public async Task Missing_Server_Api_Key_Returns_503() {
        HttpClient client = CreateClient(true);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml = "resources:\n  - kind: Server\n    name: x" });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Missing_Server_Api_Key_Returns_503_Even_Without_Client_Header() {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml = "resources:\n  - kind: Server\n    name: x" });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
