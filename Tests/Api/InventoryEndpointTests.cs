using System.Net;
using System.Net.Http.Json;
using RackPeek.Domain.Api;
using Xunit.Abstractions;

namespace Tests.Api;

public class InventoryEndpointTests(ITestOutputHelper output) : ApiTestBase(output) {
    [Fact]
    public async Task DryRun_Add_New_Resource_Does_Not_Persist() {
        HttpClient client = CreateClient(true);

        var yaml = """
                   resources:
                     - name: example-server
                       kind: Server
                   """;

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new {
                yaml,
                dryRun = true
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ImportYamlResponse? result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Single(result!.Added);
        Assert.Contains("example-server", result.Added);

        // Call again — still should be "added" because dry run did not persist
        HttpResponseMessage response2 = await client.PostAsJsonAsync("/api/inventory",
            new {
                yaml,
                dryRun = true
            });

        ImportYamlResponse? result2 = await response2.Content.ReadFromJsonAsync<ImportYamlResponse>();
        Assert.Single(result2!.Added);
    }

    [Fact]
    public async Task Merge_Add_New_Resource_Persists() {
        HttpClient client = CreateClient(true);

        var yaml = """
                   version: 2
                   resources:
                     - kind: Server
                       name: server-merge
                       
                   """;

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new {
                Yaml = yaml,
                mode = "Merge"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ImportYamlResponse? result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Single(result!.Added);

        // Now second call should detect no change
        HttpResponseMessage response2 = await client.PostAsJsonAsync("/api/inventory",
            new {
                yaml,
                dryRun = true
            });

        ImportYamlResponse? result2 = await response2.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Empty(result2!.Added);
        Assert.Empty(result2.Updated);
        Assert.Empty(result2.Replaced);
    }

    [Fact]
    public async Task Merge_Updates_Existing_Resource() {
        HttpClient client = CreateClient(true);

        var initial = """
                      version: 2
                      resources:
                      - kind: Server
                        name: server-update
                        ipmi: true
                      """;

        await client.PostAsJsonAsync("/api/inventory",
            new { Yaml = initial });

        var update = """
                     version: 2
                     resources:
                     - kind: Server
                       name: server-update
                       ipmi: false
                     """;

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new {
                Yaml = update,
                mode = "Merge"
            });

        ImportYamlResponse? result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Single(result!.Updated);
        Assert.Contains("server-update", result.Updated);
    }

    [Fact]
    public async Task Replace_Replaces_Existing_Resource() {
        HttpClient client = CreateClient(true);

        var initial = """
                      resources:
                        - kind: Server
                          name: server-replace
                          ipmi: true
                      """;

        await client.PostAsJsonAsync("/api/inventory",
            new { yaml = initial });

        var replace = """
                      resources:
                        - kind: Server
                          name: server-replace
                      """;

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new {
                yaml = replace,
                mode = "Replace"
            });

        ImportYamlResponse? result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Single(result!.Replaced);
        Assert.Contains("server-replace", result.Replaced);
    }

    [Fact]
    public async Task Invalid_Yaml_Returns_400() {
        HttpClient client = CreateClient(true);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new {
                yaml = "not: valid: yaml:"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Missing_Resources_Section_Returns_400() {
        HttpClient client = CreateClient(true);

        var yaml = """
                   somethingElse:
                     - name: test
                   """;

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Accepts_Json_Root_Input() {
        HttpClient client = CreateClient(true);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new {
                json = new {
                    version = 1,
                    resources = new[]
                    {
                        new { kind = "Server", name = "json-server" }
                    }
                }
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ImportYamlResponse? result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Single(result!.Added);
        Assert.Contains("json-server", result.Added);
    }

    [Fact]
    public async Task Requires_Api_Key() {
        HttpClient client = CreateClient();

        var yaml = """
                   resources:
                     - name: no-auth
                       kind: Server
                   """;

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Import_Full_Config_Works() {
        HttpClient client = CreateClient(true);

        var yaml = await File.ReadAllTextAsync("TestConfigs/v2/11-demo-config.yaml");
        // Put your big sample YAML in TestData folder

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ImportYamlResponse? result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.True(result!.Added.Count > 10);
        Assert.Empty(result.Updated);
        Assert.Empty(result.Replaced);
    }

    [Fact]
    public async Task Import_Full_Config_Twice_Is_Idempotent() {
        HttpClient client = CreateClient(true);
        var yaml = await File.ReadAllTextAsync("TestConfigs/v2/11-demo-config.yaml");

        await client.PostAsJsonAsync("/api/inventory", new { yaml });

        HttpResponseMessage response2 = await client.PostAsJsonAsync("/api/inventory",
            new { yaml, dryRun = true });

        ImportYamlResponse? result2 = await response2.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Empty(result2!.Added);
        Assert.Empty(result2.Updated);
        Assert.Empty(result2.Replaced);
    }

    [Fact]
    public async Task Merge_Updates_Nested_Object() {
        HttpClient client = CreateClient(true);

        var initial = """
                      version: 2
                      resources:
                        - kind: Server
                          name: nested-test
                          ram:
                            size: 64
                            mts: 2666
                      """;

        await client.PostAsJsonAsync("/api/inventory", new { yaml = initial });

        var update = """
                     version: 2
                     resources:
                       - kind: Server
                         name: nested-test
                         ram:
                           size: 128
                     """;

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml = update, mode = "Merge" });

        ImportYamlResponse? result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Single(result!.Updated);
    }

    [Fact]
    public async Task Merge_Does_Not_Clear_List_When_Empty() {
        HttpClient client = CreateClient(true);

        var initial = """
                      resources:
                        - kind: Server
                          name: drive-test
                          drives:
                            - type: ssd
                              size: 1024
                      """;

        await client.PostAsJsonAsync("/api/inventory", new { yaml = initial });

        var update = """
                     resources:
                       - kind: Server
                         name: drive-test
                         drives: []
                     """;

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml = update, mode = "Merge" });

        ImportYamlResponse? result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        // Should NOT count as update because empty list ignored
        Assert.Empty(result!.Updated);
    }

    [Fact]
    public async Task Replace_Clears_List() {
        HttpClient client = CreateClient(true);

        var initial = """
                      resources:
                        - kind: Server
                          name: replace-drive-test
                          drives:
                            - type: ssd
                              size: 1024
                      """;

        await client.PostAsJsonAsync("/api/inventory", new { yaml = initial });

        var replace = """
                      resources:
                        - kind: Server
                          name: replace-drive-test
                          drives: []
                      """;

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml = replace, mode = "Replace" });

        ImportYamlResponse? result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Single(result!.Replaced);
    }

    [Fact]
    public async Task Type_Change_Forces_Replace() {
        HttpClient client = CreateClient(true);

        var initial = """
                      version: 2
                      resources:
                        - kind: Server
                          name: polymorph-test
                      """;

        await client.PostAsJsonAsync("/api/inventory", new { yaml = initial });

        var update = """
                     version: 2
                     resources:
                       - kind: Firewall
                         name: polymorph-test
                     """;

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml = update, mode = "Merge" });

        ImportYamlResponse? result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Single(result!.Replaced);
    }

    [Fact]
    public async Task Name_Matching_Is_Case_Insensitive() {
        HttpClient client = CreateClient(true);

        var initial = """
                      resources:
                        - kind: Server
                          name: CaseTest
                      """;

        await client.PostAsJsonAsync("/api/inventory", new { yaml = initial });

        var update = """
                     resources:
                       - kind: Server
                         name: casetest
                         ipmi: true
                     """;

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml = update });

        ImportYamlResponse? result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Single(result!.Updated);
    }

    [Fact]
    public async Task Multiple_Resources_Are_Processed() {
        HttpClient client = CreateClient(true);

        var yaml = """
                   resources:
                     - kind: Server
                       name: multi-1
                     - kind: Firewall
                       name: multi-2
                   """;

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml });

        ImportYamlResponse? result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Equal(2, result!.Added.Count);
    }

    [Fact]
    public async Task DryRun_Replace_Does_Not_Persist() {
        HttpClient client = CreateClient(true);

        var initial = """
                      resources:
                        - kind: Server
                          name: dry-replace
                          ipmi: true
                      """;

        await client.PostAsJsonAsync("/api/inventory", new { yaml = initial });

        var replace = """
                      resources:
                        - kind: Server
                          name: dry-replace
                      """;

        await client.PostAsJsonAsync("/api/inventory",
            new { yaml = replace, mode = "Replace", dryRun = true });

        HttpResponseMessage check = await client.PostAsJsonAsync("/api/inventory",
            new { yaml = replace, mode = "Replace", dryRun = true });

        ImportYamlResponse? result = await check.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Single(result!.Replaced);
    }


    [Fact]
    public async Task Providing_Both_Yaml_And_Json_Returns_400() {
        HttpClient client = CreateClient(true);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new {
                yaml = "resources: []",
                json = new { resources = Array.Empty<object>() }
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Empty_Request_Returns_400() {
        HttpClient client = CreateClient(true);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Version_1_Config_Is_Accepted() {
        HttpClient client = CreateClient(true);

        var yaml = """
                   version: 1
                   resources:
                     - kind: Server
                       name: v1-server
                   """;

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory", new { yaml });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ImportYamlResponse? result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();
        Assert.Single(result!.Added);
    }

    [Fact]
    public async Task Replace_Removes_Existing_Fields() {
        HttpClient client = CreateClient(true);

        var initial = """
                      resources:
                        - kind: Server
                          name: destructive-test
                          ipmi: true
                      """;

        await client.PostAsJsonAsync("/api/inventory", new { yaml = initial });

        var replace = """
                      resources:
                        - kind: Server
                          name: destructive-test
                      """;

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml = replace, mode = "Replace" });

        ImportYamlResponse? result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Single(result!.Replaced);

        Assert.Contains("destructive-test", result.OldYaml.Keys);
        Assert.Contains("destructive-test", result.NewYaml.Keys);
    }

    [Fact]
    public async Task Merge_Does_Not_Affect_Unspecified_Resources() {
        HttpClient client = CreateClient(true);

        var full = await File.ReadAllTextAsync("TestConfigs/v2/11-demo-config.yaml");
        await client.PostAsJsonAsync("/api/inventory", new { yaml = full });

        var update = """
                     resources:
                       - kind: Server
                         name: proxmox-node01
                         ipmi: false
                     """;

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml = update, mode = "Merge" });

        ImportYamlResponse? result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Single(result!.Updated);
        Assert.DoesNotContain("proxmox-node02", result.Updated);
    }

    [Fact]
    public async Task Json_Input_Resolves_Polymorphic_Resource() {
        HttpClient client = CreateClient(true);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new {
                json = new {
                    version = 2,
                    resources = new[]
                    {
                        new { kind = "Firewall", name = "json-fw", model = "Test" }
                    }
                }
            });

        ImportYamlResponse? result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Single(result!.Added);
        Assert.Contains("json-fw", result.Added);
    }

    [Fact]
    public async Task Large_Config_Is_Fully_Idempotent() {
        HttpClient client = CreateClient(true);
        var yaml = await File.ReadAllTextAsync("TestConfigs/v2/11-demo-config.yaml");

        await client.PostAsJsonAsync("/api/inventory", new { yaml });

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml });

        ImportYamlResponse? result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Empty(result!.Added);
        Assert.Empty(result.Updated);
        Assert.Empty(result.Replaced);
    }

    [Fact]
    public async Task Unknown_Kind_Returns_400() {
        HttpClient client = CreateClient(true);

        var yaml = """
                   resources:
                     - kind: UnknownThing
                       name: mystery
                   """;

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory", new { yaml });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DryRun_Does_Not_Persist_Snapshots() {
        HttpClient client = CreateClient(true);

        var yaml = """
                   resources:
                     - kind: Server
                       name: dry-snapshot
                   """;

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml, dryRun = true });

        ImportYamlResponse? result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Empty(result!.OldYaml);
    }


    [Fact]
    public async Task Reordering_List_Does_Not_Count_As_Update() {
        HttpClient client = CreateClient(true);

        var initial = """
                      resources:
                        - kind: Server
                          name: order-test
                          drives:
                            - type: ssd
                              size: 1024
                            - type: hdd
                              size: 4096
                      """;

        await client.PostAsJsonAsync("/api/inventory", new { yaml = initial });

        var reordered = """
                        resources:
                          - kind: Server
                            name: order-test
                            drives:
                              - type: hdd
                                size: 4096
                              - type: ssd
                                size: 1024
                        """;

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml = reordered });

        ImportYamlResponse? result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Single(result!.Updated);
        Assert.Contains("order-test", result.Updated);
    }

    [Fact]
    public async Task Duplicate_Names_In_Same_Request_Returns_400() {
        HttpClient client = CreateClient(true);

        var yaml = """
                   resources:
                     - kind: Server
                       name: dup
                     - kind: Server
                       name: dup
                   """;

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory", new { yaml });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
