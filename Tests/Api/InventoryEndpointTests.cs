using System.Net;
using System.Net.Http.Json;
using RackPeek.Domain.Api;
using Xunit.Abstractions;

namespace Tests.Api;

public class InventoryEndpointTests(ITestOutputHelper output) : ApiTestBase(output)
{
    [Fact]
    public async Task DryRun_Add_New_Resource_Does_Not_Persist()
    {
        var client = CreateClient(withApiKey: true);

        var yaml = """
        resources:
          - name: example-server
            kind: Server
        """;

        var response = await client.PostAsJsonAsync("/api/inventory",
            new
            {
                yaml,
                dryRun = true
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Single(result!.Added);
        Assert.Contains("example-server", result.Added);

        // Call again — still should be "added" because dry run did not persist
        var response2 = await client.PostAsJsonAsync("/api/inventory",
            new
            {
                yaml,
                dryRun = true
            });

        var result2 = await response2.Content.ReadFromJsonAsync<ImportYamlResponse>();
        Assert.Single(result2!.Added);
    }
    
    [Fact]
    public async Task Merge_Add_New_Resource_Persists()
    {
        var client = CreateClient(withApiKey: true);

        var yaml = """
        version: 2
        resources:
          - kind: Server
            name: server-merge
            
        """;

        var response = await client.PostAsJsonAsync("/api/inventory",
            new
            {
                Yaml = yaml,
                mode = "Merge"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Single(result!.Added);

        // Now second call should detect no change
        var response2 = await client.PostAsJsonAsync("/api/inventory",
            new
            {
                yaml,
                dryRun = true
            });

        var result2 = await response2.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Empty(result2!.Added);
        Assert.Empty(result2.Updated);
        Assert.Empty(result2.Replaced);
    }
    
    [Fact]
    public async Task Merge_Updates_Existing_Resource()
    {
        var client = CreateClient(withApiKey: true);

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

        var response = await client.PostAsJsonAsync("/api/inventory",
            new
            {
                Yaml = update,
                mode = "Merge"
            });

        var result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Single(result!.Updated);
        Assert.Contains("server-update", result.Updated);
    }
    
    [Fact]
    public async Task Replace_Replaces_Existing_Resource()
    {
        var client = CreateClient(withApiKey: true);

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

        var response = await client.PostAsJsonAsync("/api/inventory",
            new
            {
                yaml = replace,
                mode = "Replace"
            });

        var result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Single(result!.Replaced);
        Assert.Contains("server-replace", result.Replaced);
    }
    
    [Fact]
    public async Task Invalid_Yaml_Returns_400()
    {
        var client = CreateClient(withApiKey: true);

        var response = await client.PostAsJsonAsync("/api/inventory",
            new
            {
                yaml = "not: valid: yaml:",
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Missing_Resources_Section_Returns_400()
    {
        var client = CreateClient(withApiKey: true);

        var yaml = """
        somethingElse:
          - name: test
        """;

        var response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Fact]
    public async Task Accepts_Json_Root_Input()
    {
        var client = CreateClient(withApiKey: true);

        var response = await client.PostAsJsonAsync("/api/inventory",
            new
            {
                json = new
                {
                    version = 1,
                    resources = new[]
                    {
                        new { kind = "Server", name = "json-server",  }
                    }
                }
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Single(result!.Added);
        Assert.Contains("json-server", result.Added);
    }
    
    [Fact]
    public async Task Requires_Api_Key()
    {
        var client = CreateClient();

        var yaml = """
        resources:
          - name: no-auth
            kind: Server
        """;

        var response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    
    [Fact]
    public async Task Import_Full_Config_Works()
    {
        var client = CreateClient(withApiKey: true);

        var yaml = await File.ReadAllTextAsync("TestConfigs/v2/11-demo-config.yaml"); 
        // Put your big sample YAML in TestData folder

        var response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.True(result!.Added.Count > 10);
        Assert.Empty(result.Updated);
        Assert.Empty(result.Replaced);
    }
    
    [Fact]
    public async Task Import_Full_Config_Twice_Is_Idempotent()
    {
        var client = CreateClient(withApiKey: true);
        var yaml = await File.ReadAllTextAsync("TestConfigs/v2/11-demo-config.yaml"); 

        await client.PostAsJsonAsync("/api/inventory", new { yaml });

        var response2 = await client.PostAsJsonAsync("/api/inventory",
            new { yaml, dryRun = true });

        var result2 = await response2.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Empty(result2!.Added);
        Assert.Empty(result2.Updated);
        Assert.Empty(result2.Replaced);
    }
    
    [Fact]
    public async Task Merge_Updates_Nested_Object()
    {
        var client = CreateClient(withApiKey: true);

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

        var response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml = update, mode = "Merge" });

        var result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Single(result!.Updated);
    }
    
    [Fact]
    public async Task Merge_Does_Not_Clear_List_When_Empty()
    {
        var client = CreateClient(withApiKey: true);

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

        var response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml = update, mode = "Merge" });

        var result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        // Should NOT count as update because empty list ignored
        Assert.Empty(result!.Updated);
    }
    
    [Fact]
    public async Task Replace_Clears_List()
    {
        var client = CreateClient(withApiKey: true);

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

        var response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml = replace, mode = "Replace" });

        var result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Single(result!.Replaced);
    }
    
    [Fact]
    public async Task Type_Change_Forces_Replace()
    {
        var client = CreateClient(withApiKey: true);

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

        var response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml = update, mode = "Merge" });

        var result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Single(result!.Replaced);
    }
    
    [Fact]
    public async Task Name_Matching_Is_Case_Insensitive()
    {
        var client = CreateClient(withApiKey: true);

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

        var response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml = update });

        var result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Single(result!.Updated);
    }
    
    [Fact]
    public async Task Multiple_Resources_Are_Processed()
    {
        var client = CreateClient(withApiKey: true);

        var yaml = """
                   resources:
                     - kind: Server
                       name: multi-1
                     - kind: Firewall
                       name: multi-2
                   """;

        var response = await client.PostAsJsonAsync("/api/inventory",
            new { yaml });

        var result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Equal(2, result!.Added.Count);
    }
    
    [Fact]
    public async Task DryRun_Replace_Does_Not_Persist()
    {
        var client = CreateClient(withApiKey: true);

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
        
        var check = await client.PostAsJsonAsync("/api/inventory",
            new { yaml = replace, mode = "Replace", dryRun = true });

        var result = await check.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Single(result!.Replaced);
    }
    
}