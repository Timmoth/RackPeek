using System.Net;
using System.Net.Http.Json;
using RackPeek.Domain.Api;
using Xunit.Abstractions;

namespace Tests.Api;

[Collection("Process-wide static state")]
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
                     version: 3
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
                     version: 3
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
                     version: 3
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

    [Fact]
    public async Task Health_Endpoint_Is_Anonymous_And_Returns_rackpeek() {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("rackpeek", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Wrong_Api_Key_Returns_401() {
        HttpClient client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "definitely-wrong");

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml = "resources:\n  - kind: Server\n    name: x" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Bad_Request_Body_Contains_Error_Field() {
        HttpClient client = CreateClient(true);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        Dictionary<string, string>? body =
            await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        Assert.NotNull(body);
        Assert.True(body!.ContainsKey("error"));
        Assert.False(string.IsNullOrWhiteSpace(body["error"]));
    }

    [Fact]
    public async Task Added_Resource_Has_No_OldYaml_Entry() {
        HttpClient client = CreateClient(true);

        var yaml = """
                   resources:
                     - kind: Server
                       name: fresh-server
                   """;

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory", new { yaml });

        ImportYamlResponse? result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Contains("fresh-server", result!.Added);
        Assert.DoesNotContain("fresh-server", result.OldYaml.Keys);
        Assert.Contains("fresh-server", result.NewYaml.Keys);
    }

    [Fact]
    public async Task Merge_Replaces_Tags_Wholesale() {
        HttpClient client = CreateClient(true);

        var initial = """
                      resources:
                        - kind: Server
                          name: tag-merge
                          tags:
                            - alpha
                            - beta
                      """;

        await client.PostAsJsonAsync("/api/inventory", new { yaml = initial });

        var update = """
                     resources:
                       - kind: Server
                         name: tag-merge
                         tags:
                           - gamma
                     """;

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml = update, mode = "Merge" });

        ImportYamlResponse? result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Contains("tag-merge", result!.Updated);

        var newYaml = result.NewYaml["tag-merge"];
        Assert.Contains("gamma", newYaml);
        Assert.DoesNotContain("alpha", newYaml);
        Assert.DoesNotContain("beta", newYaml);
    }

    [Theory]
    [InlineData("Server")]
    [InlineData("Switch")]
    [InlineData("Router")]
    [InlineData("Firewall")]
    [InlineData("AccessPoint")]
    [InlineData("Ups")]
    [InlineData("Desktop")]
    [InlineData("Laptop")]
    [InlineData("Service")]
    [InlineData("System")]
    public async Task Documented_Kind_Discriminator_Values_Are_Accepted(string kind) {
        // The inventory-api doc lists the exact set of valid kind discriminator
        // values. Every one must be accepted as-is so the doc and runtime stay
        // in lockstep.
        HttpClient client = CreateClient(true);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new {
                json = new {
                    version = 3,
                    resources = new[]
                    {
                        new { kind, name = $"kind-probe-{kind.ToLowerInvariant()}" }
                    }
                }
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Lowercase_Kind_Is_Rejected() {
        // The discriminator is case-sensitive — the doc must use exact casing
        // or users hit 400. This test pins that contract so a future refactor
        // making the kind case-insensitive would surface here and prompt a
        // doc update.
        HttpClient client = CreateClient(true);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new {
                json = new {
                    version = 3,
                    resources = new[]
                    {
                        new { kind = "server", name = "lowercase-probe" }
                    }
                }
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Merge_Preserves_Labels_Not_In_Incoming() {
        HttpClient client = CreateClient(true);

        var initial = """
                      resources:
                        - kind: Server
                          name: label-merge
                          labels:
                            env: production
                            team: backend
                      """;

        await client.PostAsJsonAsync("/api/inventory", new { yaml = initial });

        var update = """
                     resources:
                       - kind: Server
                         name: label-merge
                         labels:
                           env: staging
                     """;

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml = update, mode = "Merge" });

        ImportYamlResponse? result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Contains("label-merge", result!.Updated);

        var newYaml = result.NewYaml["label-merge"];
        Assert.Contains("env: staging", newYaml);
        Assert.Contains("team: backend", newYaml);
        Assert.DoesNotContain("env: production", newYaml);
    }

    [Fact]
    public async Task Import_Persists_And_Updates_Connections() {
        HttpClient client = CreateClient(true);

        var initial = """
                      version: 3
                      resources:
                      - kind: Switch
                        ports:
                        - type: rj45
                          speed: 1
                          count: 8
                        name: switch1
                      - kind: Server
                        ports:
                        - type: rj45
                          speed: 1
                          count: 1
                        name: server1
                      connections:
                      - a:
                          resource: server1
                          portGroup: 0
                          portIndex: 0
                        b:
                          resource: switch1
                          portGroup: 0
                          portIndex: 0
                      """;

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml = initial, mode = "Merge" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        ImportYamlResponse? result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();
        Assert.Single(result!.ConnectionsAdded);
        Assert.Empty(result.ConnectionsRemoved);

        // Re-importing the same config is idempotent
        HttpResponseMessage response2 = await client.PostAsJsonAsync("/api/inventory",
            new { yaml = initial, dryRun = true, mode = "Merge" });
        ImportYamlResponse? result2 = await response2.Content.ReadFromJsonAsync<ImportYamlResponse>();
        Assert.Empty(result2!.ConnectionsAdded);
        Assert.Empty(result2.ConnectionsRemoved);

        // Adjusting the switch port replaces the old connection (#308)
        var adjusted = initial.Replace(
            """
                resource: switch1
                portGroup: 0
                portIndex: 0
            """,
            """
                resource: switch1
                portGroup: 0
                portIndex: 3
            """);

        HttpResponseMessage response3 = await client.PostAsJsonAsync("/api/inventory",
            new { yaml = adjusted, mode = "Merge" });
        ImportYamlResponse? result3 = await response3.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Single(result3!.ConnectionsAdded);
        Assert.Contains("switch1[0.3]", result3.ConnectionsAdded[0]);
        Assert.Single(result3.ConnectionsRemoved);
        Assert.Contains("switch1[0.0]", result3.ConnectionsRemoved[0]);

        // And the adjusted state is now stable
        HttpResponseMessage response4 = await client.PostAsJsonAsync("/api/inventory",
            new { yaml = adjusted, dryRun = true, mode = "Merge" });
        ImportYamlResponse? result4 = await response4.Content.ReadFromJsonAsync<ImportYamlResponse>();
        Assert.Empty(result4!.ConnectionsAdded);
        Assert.Empty(result4.ConnectionsRemoved);
    }

    [Fact]
    public async Task Import_Without_Connections_Section_Keeps_Existing_Connections() {
        HttpClient client = CreateClient(true);

        var initial = """
                      version: 3
                      resources:
                      - kind: Switch
                        ports:
                        - type: rj45
                          speed: 1
                          count: 8
                        name: sw-keep
                      - kind: Server
                        ports:
                        - type: rj45
                          speed: 1
                          count: 1
                        name: srv-keep
                      connections:
                      - a:
                          resource: srv-keep
                          portGroup: 0
                          portIndex: 0
                        b:
                          resource: sw-keep
                          portGroup: 0
                          portIndex: 0
                      """;

        await client.PostAsJsonAsync("/api/inventory", new { yaml = initial, mode = "Merge" });

        // An import that says nothing about connections must not drop them
        var resourcesOnly = """
                            resources:
                            - kind: Server
                              name: srv-keep
                              notes: updated
                            """;

        await client.PostAsJsonAsync("/api/inventory", new { yaml = resourcesOnly, mode = "Merge" });

        // The original connection still exists: re-importing the initial
        // config reports no connection changes.
        HttpResponseMessage check = await client.PostAsJsonAsync("/api/inventory",
            new { yaml = initial, dryRun = true, mode = "Merge" });
        ImportYamlResponse? result = await check.Content.ReadFromJsonAsync<ImportYamlResponse>();
        Assert.Empty(result!.ConnectionsAdded);
        Assert.Empty(result.ConnectionsRemoved);
    }

    [Fact]
    public async Task Merge_Other_Hardware_Persists() {
        HttpClient client = CreateClient(true);

        var yaml = """
                   resources:
                     - kind: Other
                       name: radio-merge
                       model: Building Bridge XG
                       description: Microwave radio bridge
                   """;

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory",
            new {
                Yaml = yaml,
                mode = "Merge"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ImportYamlResponse? result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Contains("radio-merge", result!.Added);

        var update = """
                     resources:
                       - kind: Other
                         name: radio-merge
                         description: Site-to-site connectivity
                     """;

        HttpResponseMessage response2 = await client.PostAsJsonAsync("/api/inventory",
            new { yaml = update, mode = "Merge" });

        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        ImportYamlResponse? result2 = await response2.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Contains("radio-merge", result2!.Updated);

        var newYaml = result2.NewYaml["radio-merge"];
        Assert.Contains("model: Building Bridge XG", newYaml);
        Assert.Contains("description: Site-to-site connectivity", newYaml);
    }
}
