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
        var output = await YamlCliTestHost.RunAsync(inputArgs, fs.Root, outputHelper, "config.yaml");

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
                       name: immich

                     """, yaml);
        (output, yaml) = await ExecuteAsync("systems", "add", "vm01");

        // Update system
        (output, yaml) = await ExecuteAsync("services", "set", "immich", "--ip", "192.168.10.14", "--port", "80",
            "--protocol", "TCP", "--url", "http://timmoth.lan:80", "--runs-on", "vm01");

        Assert.Equal("Service 'immich' updated.\n", output);

        outputHelper.WriteLine(yaml);
        Assert.Equal("""
                     resources:
                     - kind: Service
                       network:
                         ip: 192.168.10.14
                         port: 80
                         protocol: TCP
                         url: http://timmoth.lan:80
                       name: immich
                       runsOn: vm01
                     - kind: System
                       name: vm01

                     """, yaml);

        // Delete system
        (output, yaml) = await ExecuteAsync("services", "del", "immich");
        Assert.Equal("""
                     Service 'immich' deleted.

                     """, output);

        Assert.Equal("""
                     resources:
                     - kind: System
                       name: vm01

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

        (output, yaml) = await ExecuteAsync("servers", "add", "c6400");
        Assert.Equal("Server 'c6400' added.\n", output);

        (output, yaml) = await ExecuteAsync("systems", "add", "vm01");
        Assert.Equal("System 'vm01' added.\n", output);
        (output, yaml) = await ExecuteAsync("systems", "set", "vm01", "--runs-on", "c6400");


        // Update system
        (output, yaml) = await ExecuteAsync("services", "set", "immich", "--ip", "192.168.10.14", "--port", "80",
            "--protocol", "TCP", "--url", "http://timmoth.lan:80", "--runs-on", "vm01");

        Assert.Equal("Service 'immich' updated.\n", output);

        // Get system by name
        (output, yaml) = await ExecuteAsync("services", "get", "immich");
        Assert.Equal("""
                     immich  Ip: 192.168.10.14, Port: 80, Protocol: TCP, Url: http://timmoth.lan:80, 
                     RunsOn: c6400/vm01

                     """, output);

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

        // Report systemså
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

    [Fact]
    public async Task services_subnets_cli_test()
    {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Add services
        var (output, yaml) = await ExecuteAsync("services", "add", "svc1");
        Assert.Equal("Service 'svc1' added.\n", output);

        (output, yaml) = await ExecuteAsync("services", "add", "svc2");
        Assert.Equal("Service 'svc2' added.\n", output);

        (output, yaml) = await ExecuteAsync("services", "add", "svc3");
        Assert.Equal("Service 'svc3' added.\n", output);

        // Add system + server so RunsOn resolves
        (output, yaml) = await ExecuteAsync("systems", "add", "vm01");
        Assert.Equal("System 'vm01' added.\n", output);

        (output, yaml) = await ExecuteAsync("servers", "add", "c6400");
        Assert.Equal("Server 'c6400' added.\n", output);

        (output, yaml) = await ExecuteAsync("systems", "set", "vm01", "--runs-on", "c6400");
        Assert.Equal("System 'vm01' updated.\n", output);

        // Assign IPs
        (output, yaml) = await ExecuteAsync("services", "set", "svc1", "--ip", "192.168.10.10", "--port", "80",
            "--protocol", "TCP", "--runs-on", "vm01");
        Assert.Equal("Service 'svc1' updated.\n", output);

        (output, yaml) = await ExecuteAsync("services", "set", "svc2", "--ip", "192.168.10.20", "--port", "443",
            "--protocol", "TCP", "--runs-on", "vm01");
        Assert.Equal("Service 'svc2' updated.\n", output);

        (output, yaml) = await ExecuteAsync("services", "set", "svc3", "--ip", "10.0.0.5", "--port", "8080",
            "--protocol", "TCP", "--runs-on", "vm01");
        Assert.Equal("Service 'svc3' updated.\n", output);

        // -----------------------------
        // Test CIDR filter mode
        // -----------------------------
        (output, yaml) = await ExecuteAsync("services", "subnets", "--cidr", "192.168.10.0/24");

        Assert.Equal("""
                     Services in 192.168.10.0/24
                     ╭──────┬───────────────┬─────────╮
                     │ Name │ IP            │ Runs On │
                     ├──────┼───────────────┼─────────┤
                     │ svc1 │ 192.168.10.10 │ vm01    │
                     │ svc2 │ 192.168.10.20 │ vm01    │
                     ╰──────┴───────────────┴─────────╯

                     """, output);

        // -----------------------------
        // Test subnet summary mode
        // -----------------------------
        (output, yaml) = await ExecuteAsync("services", "subnets");

        Assert.Contains("Subnet", output);
        Assert.Contains("Utilization", output);
        Assert.Contains("192.168.10.0/24", output);
        Assert.Contains("10.0.0.0/24", output);
    }
}