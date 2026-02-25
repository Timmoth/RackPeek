using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.ServiceTests;

[Collection("Yaml CLI tests")]
public class ServiceWorkflowTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture>
{
    private async Task<(string output, string yaml)> ExecuteAsync(params string[] args)
    {
        outputHelper.WriteLine($"rpk {string.Join(" ", args)}");

        var output = await YamlCliTestHost.RunAsync(
            args,
            fs.Root,
            outputHelper,
            "config.yaml");

        outputHelper.WriteLine(output);

        var yaml = await File.ReadAllTextAsync(Path.Combine(fs.Root, "config.yaml"));
        return (output, yaml);
    }

    [Fact]
    public async Task services_cli_workflow_test()
    {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Add parent system
        await ExecuteAsync("systems", "add", "sys01");

        // Add service
        var (output, yaml) = await ExecuteAsync("services", "add", "svc01");
        Assert.Equal("Service 'svc01' added.\n", output);
        Assert.Contains("name: svc01", yaml);

        // Update service
        (output, yaml) = await ExecuteAsync(
            "services", "set", "svc01",
            "--ip", "10.0.0.5",
            "--port", "8080",
            "--protocol", "http",
            "--url", "http://10.0.0.5:8080",
            "--runs-on", "sys01"
        );
        Assert.Equal("Service 'svc01' updated.\n", output);
        outputHelper.WriteLine(yaml);

        Assert.Equal("""
                     version: 2
                     resources:
                     - kind: System
                       name: sys01
                     - kind: Service
                       network:
                         ip: 10.0.0.5
                         port: 8080
                         protocol: http
                         url: http://10.0.0.5:8080
                       name: svc01
                       runsOn:
                       - sys01

                     """, yaml);

        // Get service
        (output, yaml) = await ExecuteAsync("services", "get", "svc01");
        Assert.Equal("svc01  Ip: 10.0.0.5, Port: 8080, Protocol: http, Url: http://10.0.0.5:8080, \nRunsOn: sys01\n", output);

        // List services (strict table)
        (output, yaml) = await ExecuteAsync("services", "list");
        Assert.Equal("""
                     ╭───────┬──────────┬──────┬──────────┬──────────────────────┬─────────╮
                     │ Name  │ Ip       │ Port │ Protocol │ Url                  │ Runs On │
                     ├───────┼──────────┼──────┼──────────┼──────────────────────┼─────────┤
                     │ svc01 │ 10.0.0.5 │ 8080 │ http     │ http://10.0.0.5:8080 │ sys01   │
                     ╰───────┴──────────┴──────┴──────────┴──────────────────────┴─────────╯

                     """, output);

        // Summary (strict table)
        (output, yaml) = await ExecuteAsync("services", "summary");
        Assert.Equal("""
                     ╭───────┬──────────┬──────┬──────────┬──────────────────────┬─────────╮
                     │ Name  │ Ip       │ Port │ Protocol │ Url                  │ Runs On │
                     ├───────┼──────────┼──────┼──────────┼──────────────────────┼─────────┤
                     │ svc01 │ 10.0.0.5 │ 8080 │ http     │ http://10.0.0.5:8080 │ sys01   │
                     ╰───────┴──────────┴──────┴──────────┴──────────────────────┴─────────╯

                     """, output);

        // Subnets (strict)
        (output, yaml) = await ExecuteAsync("services", "subnets");
        Assert.Equal("""
                        ╭─────────────┬──────────┬───────────────────────────────────╮
                        │ Subnet      │ Services │ Utilization                       │
                        ├─────────────┼──────────┼───────────────────────────────────┤
                        │ 10.0.0.0/24 │ 1        │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ 0% │
                        ╰─────────────┴──────────┴───────────────────────────────────╯
                        
                        """, output);

        // Describe (strict)
        (output, yaml) = await ExecuteAsync("services", "describe", "svc01");
        Assert.Equal("""
                     ╭─Service─────────────────────────────────╮
                     │ Name:      svc01                        │
                     │ Ip:        10.0.0.5                     │
                     │ Port:      8080                         │
                     │ Protocol:  http                         │
                     │ Url:       http://10.0.0.5:8080         │
                     │ Runs On:   sys01                        │
                     ╰─────────────────────────────────────────╯
                     
                     """,output);
                     
        
        // Delete service
        (output, yaml) = await ExecuteAsync("services", "del", "svc01");
        Assert.Equal("""
                     Service 'svc01' deleted.

                     """, output);
    }
}
