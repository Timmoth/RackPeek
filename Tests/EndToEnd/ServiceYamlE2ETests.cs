using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd;

[Collection("Yaml CLI tests")]
public class ServiceYamlE2ETests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture>
{
    private async Task<(string, string)> ExecuteAsync(params string[] args)
    {
        outputHelper.WriteLine($"rpk {string.Join(" ", args)}");

        var inputArgs = args.ToArray();
        var output = await YamlCliTestHost.RunAsync(
            inputArgs,
            fs.Root,
            outputHelper);

        outputHelper.WriteLine(output);

        var yaml = await File.ReadAllTextAsync(Path.Combine(fs.Root, "config.yaml"));
        return (output, yaml);
    }

      [Fact]
    public async Task services_cli_yaml_test()
    {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Add system
        var (output, yaml) = await ExecuteAsync("services", "add", "immich");
        Assert.Equal("Service 'immich' added.\n", output);
        Assert.Equal("""
                     resources:
                     - kind: Service
                       network: 
                       runsOn: 
                       name: immich
                       tags: 

                     """, yaml);
        
        // Update system
        (output, yaml) = await ExecuteAsync(
            "services", "set", "immich",
            "--ip", "192.168.10.14",
            "--port", "80",
            "--protocol", "TCP",
            "--url", "http://timmoth.lan:80",
            "--runs-on", "vm01"
        );

        Assert.Equal("Service 'immich' updated.\n", output);

        Assert.Equal("""
                     resources:
                     - kind: Service
                       network:
                         ip: 192.168.10.14
                         port: 80
                         protocol: TCP
                         url: http://timmoth.lan:80
                       runsOn: vm01
                       name: immich
                       tags: 

                     """, yaml);

        // Delete system
        (output, yaml) = await ExecuteAsync("services", "del", "immich");
        Assert.Equal("""
                     Service 'immich' deleted.

                     """, output);
        
        Assert.Equal("""
                     resources: []
                     
                     """, yaml);
        
        // Ensure list is empty
        (output, yaml) = await ExecuteAsync("services", "list");
        Assert.Equal("""
                     No Services found.

                     """, output);
    }
    
    [Fact]
    public async Task services_cli_workflow_test()
    {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Add system
        var (output, yaml) = await ExecuteAsync("services", "add", "immich");
        Assert.Equal("Service 'immich' added.\n", output);

        (output, yaml) = await ExecuteAsync("systems", "add", "vm01");
        Assert.Equal("System 'vm01' added.\n", output);
        (output, yaml) = await ExecuteAsync(
            "systems", "set", "vm01",
            "--runs-on", "c6400"
        );
        
        (output, yaml) = await ExecuteAsync("servers", "add", "c6400");
        Assert.Equal("Server 'c6400' added.\n", output);
        
        // Update system
        (output, yaml) = await ExecuteAsync(
            "services", "set", "immich",
            "--ip", "192.168.10.14",
            "--port", "80",
            "--protocol", "TCP",
            "--url", "http://timmoth.lan:80",
            "--runs-on", "vm01"
        );

        Assert.Equal("Service 'immich' updated.\n", output);

        // Get system by name
        (output, yaml) = await ExecuteAsync("services", "get", "immich");
        Assert.Equal(
            """
            immich  Ip: 192.168.10.14, Port: 80, Protocol: TCP, Url: http://timmoth.lan:80, 
            RunsOn: c6400/vm01
            
            """
            ,
            output);

        // List systems
        (output, yaml) = await ExecuteAsync("services", "list");
        Assert.Equal("""
                     ╭────────┬───────────────┬──────┬──────────┬──────────────────────┬────────────╮
                     │ Name   │ Ip            │ Port │ Protocol │ Url                  │ Runs On    │
                     ├────────┼───────────────┼──────┼──────────┼──────────────────────┼────────────┤
                     │ immich │ 192.168.10.14 │ 80   │ TCP      │ http://timmoth.lan:8 │ c6400/vm01 │
                     │        │               │      │          │ 0                    │            │
                     ╰────────┴───────────────┴──────┴──────────┴──────────────────────┴────────────╯
                     
                     """, output);

        // Report systems
        (output, yaml) = await ExecuteAsync("services", "summary");
        Assert.Equal("""
                     ╭────────┬───────────────┬──────┬──────────┬──────────────────────┬────────────╮
                     │ Name   │ Ip            │ Port │ Protocol │ Url                  │ Runs On    │
                     ├────────┼───────────────┼──────┼──────────┼──────────────────────┼────────────┤
                     │ immich │ 192.168.10.14 │ 80   │ TCP      │ http://timmoth.lan:8 │ c6400/vm01 │
                     │        │               │      │          │ 0                    │            │
                     ╰────────┴───────────────┴──────┴──────────┴──────────────────────┴────────────╯
                     
                     """, output);

        // Delete system
        (output, yaml) = await ExecuteAsync("services", "del", "immich");
        Assert.Equal("""
                     Service 'immich' deleted.

                     """, output);

        // Ensure list is empty
        (output, yaml) = await ExecuteAsync("services", "list");
        Assert.Equal("""
                     No Services found.

                     """, output);
    }
}