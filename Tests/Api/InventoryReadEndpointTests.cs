using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RackPeek.Domain.Api;
using Xunit.Abstractions;

namespace Tests.Api;

[Collection("Process-wide static state")]
public class InventoryReadEndpointTests(ITestOutputHelper output) : ApiTestBase(output) {
    private const string _seedYaml = """
                                    resources:
                                      - kind: Server
                                        name: srv-read
                                        tags:
                                          - homelab
                                        ports:
                                          - type: rj45
                                            speed: 1
                                            count: 2
                                      - kind: Switch
                                        name: sw-read
                                        ports:
                                          - type: rj45
                                            speed: 1
                                            count: 4
                                      - kind: AccessPoint
                                        name: ap-read
                                        model: U6 Lite
                                      - kind: System
                                        name: pve-read
                                        runsOn:
                                          - srv-read
                                        type: hypervisor
                                        os: Proxmox VE 9
                                      - kind: Service
                                        name: svc-read
                                        runsOn:
                                          - pve-read
                                        network:
                                          ip: 10.0.0.5
                                          port: 8080
                                    connections:
                                      - a:
                                          resource: srv-read
                                          portGroup: 0
                                          portIndex: 0
                                        b:
                                          resource: sw-read
                                          portGroup: 0
                                          portIndex: 0
                                    """;

    private async Task SeedAsync(HttpClient client) {
        HttpResponseMessage seed = await client.PostAsJsonAsync("/api/inventory", new { yaml = _seedYaml });
        Assert.True(seed.StatusCode == HttpStatusCode.OK,
            $"seed failed: {await seed.Content.ReadAsStringAsync()}");
    }

    [Fact]
    public async Task Get_Inventory_Without_Api_Key_Returns_401() {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/inventory");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_Summary_Without_Api_Key_Returns_401() {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/inventory/summary");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_Inventory_Returns_Full_Document() {
        HttpClient client = CreateClient(true);
        await SeedAsync(client);

        HttpResponseMessage response = await client.GetAsync("/api/inventory");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument doc = await ReadJsonAsync(response);
        JsonElement root = doc.RootElement;

        Assert.Equal(RackPeek.Domain.Persistence.Yaml.RackPeekConfigMigrationDeserializer.ListOfMigrations.Count,
            root.GetProperty("version").GetInt32());
        Assert.Equal(5, root.GetProperty("resources").GetArrayLength());
        Assert.Equal(1, root.GetProperty("connections").GetArrayLength());

        JsonElement server = root.GetProperty("resources").EnumerateArray()
            .First(r => r.GetProperty("name").GetString() == "srv-read");
        Assert.Equal("Server", server.GetProperty("kind").GetString());
        Assert.Contains("homelab", server.GetProperty("tags").EnumerateArray()
            .Select(t => t.GetString()));
        // camelCase field names, matching the published schema
        Assert.Equal("rj45", server.GetProperty("ports")[0].GetProperty("type").GetString());

        JsonElement connection = root.GetProperty("connections")[0];
        Assert.Equal("srv-read", connection.GetProperty("a").GetProperty("resource").GetString());
        Assert.Equal("sw-read", connection.GetProperty("b").GetProperty("resource").GetString());
    }

    [Fact]
    public async Task Get_Inventory_Emits_Json_Discriminator_Casing() {
        // The Resource.Kind property and the URL/routing kind say "Accesspoint";
        // the JSON discriminator the import endpoint requires is "AccessPoint".
        // The document must always carry the importable spelling.
        HttpClient client = CreateClient(true);
        await SeedAsync(client);

        using JsonDocument doc = await ReadJsonAsync(await client.GetAsync("/api/inventory"));

        JsonElement ap = doc.RootElement.GetProperty("resources").EnumerateArray()
            .First(r => r.GetProperty("name").GetString() == "ap-read");

        Assert.Equal("AccessPoint", ap.GetProperty("kind").GetString());
    }

    [Fact]
    public async Task Get_Inventory_Document_Round_Trips_Through_Import_As_No_Op() {
        // The whole point of the read API for a companion app: fetch the document,
        // send it back unchanged, and the server reports nothing changed.
        HttpClient client = CreateClient(true);
        await SeedAsync(client);

        using JsonDocument doc = await ReadJsonAsync(await client.GetAsync("/api/inventory"));
        JsonElement document = JsonDocument.Parse(doc.RootElement.GetRawText()).RootElement;

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new {
                json = document,
                dryRun = true
            });

        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"round trip failed: {await response.Content.ReadAsStringAsync()}");

        ImportYamlResponse? result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Empty(result!.Added);
        Assert.Empty(result.Updated);
        Assert.Empty(result.Replaced);
        Assert.Empty(result.ConnectionsAdded);
        Assert.Empty(result.ConnectionsRemoved);
    }

    [Fact]
    public async Task Get_Summary_Returns_Counts() {
        HttpClient client = CreateClient(true);
        await SeedAsync(client);

        using JsonDocument doc = await ReadJsonAsync(await client.GetAsync("/api/inventory/summary"));
        JsonElement root = doc.RootElement;

        Assert.Equal(3, root.GetProperty("hardware").GetProperty("totalHardware").GetInt32());
        Assert.Equal(1, root.GetProperty("systems").GetProperty("totalSystems").GetInt32());
        Assert.Equal(1, root.GetProperty("services").GetProperty("totalServices").GetInt32());
        Assert.Equal(1, root.GetProperty("tags").GetProperty("homelab").GetInt32());
    }

    [Fact]
    public async Task Get_Search_Returns_Ranked_Matches() {
        HttpClient client = CreateClient(true);
        await SeedAsync(client);

        using JsonDocument doc =
            await ReadJsonAsync(await client.GetAsync("/api/inventory/search?q=svc-read"));
        JsonElement matches = doc.RootElement.GetProperty("matches");

        Assert.True(matches.GetArrayLength() >= 1);
        Assert.Equal("svc-read", matches[0].GetProperty("name").GetString());
        Assert.Equal("Service", matches[0].GetProperty("kind").GetString());
    }

    [Fact]
    public async Task Get_Search_With_Missing_Query_Returns_400() {
        HttpClient client = CreateClient(true);

        HttpResponseMessage response = await client.GetAsync("/api/inventory/search");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_Tree_Returns_Containment_Forest() {
        HttpClient client = CreateClient(true);
        await SeedAsync(client);

        using JsonDocument doc = await ReadJsonAsync(await client.GetAsync("/api/inventory/tree"));
        JsonElement hardware = doc.RootElement.GetProperty("hardware");

        JsonElement server = hardware.EnumerateArray()
            .First(h => h.GetProperty("hardwareName").GetString() == "srv-read");

        Assert.Equal("Server", server.GetProperty("kind").GetString());
        Assert.Single(server.GetProperty("systems").EnumerateArray());
        Assert.Contains("svc-read",
            server.GetProperty("systems")[0].GetProperty("services").EnumerateArray()
                .Select(s => s.GetString()));
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) {
        var body = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body);
    }
}
