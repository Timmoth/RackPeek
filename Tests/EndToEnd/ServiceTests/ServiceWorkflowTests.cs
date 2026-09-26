using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.ServiceTests;

[Collection("Yaml CLI tests")]
public class ServiceWorkflowTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture> {
    private async Task<(string output, string yaml)> ExecuteAsync(params string[] args) {
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
    public async Task services_cli_workflow_test() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Add parent system
        await ExecuteAsync("systems", "add", "sys01");

        // Add service
        (var output, var yaml) = await ExecuteAsync("services", "add", "svc01");
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
                     version: 4
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
                     connections: []

                     """, yaml);

        // Get service
        (output, yaml) = await ExecuteAsync("services", "get", "svc01");
        Assert.Equal("svc01  Ip: 10.0.0.5, Port: 8080, Protocol: http, Url: http://10.0.0.5:8080, \nRunsOn: sys01\n",
            output);

        // List services (flexible table check)
        (output, yaml) = await ExecuteAsync("services", "list");
        Assert.Contains("svc01", output);
        Assert.Contains("10.0.0.5", output);
        Assert.Contains("8080", output);
        Assert.Contains("http", output);
        Assert.Contains("Ip", output);
        Assert.Contains("Port", output);
        Assert.Contains("Protocol", output);
        Assert.Contains("Runs On", output);
        Assert.Contains("sys01", output);

        // Summary (flexible table check)
        (output, yaml) = await ExecuteAsync("services", "summary");
        Assert.Contains("svc01", output);
        Assert.Contains("10.0.0.5", output);
        Assert.Contains("8080", output);
        Assert.Contains("http", output);
        Assert.Contains("Ip", output);
        Assert.Contains("Port", output);
        Assert.Contains("Protocol", output);
        Assert.Contains("Runs On", output);
        Assert.Contains("sys01", output);

        // Subnets (flexible)
        (output, yaml) = await ExecuteAsync("services", "subnets");
        Assert.Contains("10.0.0.0/24", output);
        Assert.Contains("Services", output);
        Assert.Contains("Utilization", output);

        // Describe (flexible)
        (output, yaml) = await ExecuteAsync("services", "describe", "svc01");
        Assert.Contains("svc01", output);
        Assert.Contains("10.0.0.5", output);
        Assert.Contains("8080", output);
        Assert.Contains("http", output);
        Assert.Contains("Ip", output);
        Assert.Contains("Protocol", output);
        Assert.Contains("Runs On", output);
        Assert.Contains("sys01", output);


        // Delete service
        (output, yaml) = await ExecuteAsync("services", "del", "svc01");
        Assert.Equal("""
                     Service 'svc01' deleted.

                     """, output);
    }
}
