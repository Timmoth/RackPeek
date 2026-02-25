using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.ServerTests;

[Collection("Yaml CLI tests")]
public class ServerWorkflowTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
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
    public async Task servers_cli_workflow_test()
    {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Add server
        var (output, yaml) = await ExecuteAsync("servers", "add", "srv01");
        Assert.Equal("Server 'srv01' added.\n", output);
        Assert.Contains("name: srv01", yaml);

        // Update server
        (output, yaml) = await ExecuteAsync(
            "servers", "set", "srv01",
            "--ram", "128",
            "--ram_mts", "3200",
            "--ipmi", "True"
        );
        Assert.Equal("Server 'srv01' updated.\n", output);

        Assert.Equal("""
                     version: 2
                     resources:
                     - kind: Server
                       ram:
                         size: 128
                         mts: 3200
                       ipmi: true
                       name: srv01

                     """, yaml);

        // Add CPU
        (output, yaml) = await ExecuteAsync(
            "servers", "cpu", "add", "srv01",
            "--model", "Intel Xeon Silver 4310",
            "--cores", "12",
            "--threads", "24"
        );
        Assert.Equal("CPU added to 'srv01'.\n", output);

        // Add Drive
        (output, yaml) = await ExecuteAsync(
            "servers", "drive", "add", "srv01",
            "--type", "ssd",
            "--size", "1024"
        );
        Assert.Equal("Drive added to 'srv01'.\n", output);

        // Add GPU
        (output, yaml) = await ExecuteAsync(
            "servers", "gpu", "add", "srv01",
            "--model", "NVIDIA A2000",
            "--vram", "6"
        );
        Assert.Equal("GPU added to 'srv01'.\n", output);

        // Add NIC
        (output, yaml) = await ExecuteAsync(
            "servers", "nic", "add", "srv01",
            "--type", "RJ45",
            "--speed", "10",
            "--ports", "2"
        );
        Assert.Equal("NIC added to 'srv01'.\n", output);

        // Get server
        (output, yaml) = await ExecuteAsync("servers", "get", "srv01");
        Assert.Equal(
            "srv01  RAM: 128 GB, IPMI: yes\n",
            output
        );
        

        
    // Summary (strict table)
        (output, yaml) = await ExecuteAsync("servers", "summary");
        
        Assert.Equal("""
                     ╭───────┬────────────┬───────┬────────┬────────────┬───────┬────────────┬──────╮
                     │ Name  │ CPU        │ C/T   │ RAM    │ Storage    │ NICs  │ GPUs       │ IPMI │
                     ├───────┼────────────┼───────┼────────┼────────────┼───────┼────────────┼──────┤
                     │ srv01 │ 1× Intel   │ 12/24 │ 128 GB │ 1024 GB    │ 2×10G │ 1× NVIDIA  │ yes  │
                     │       │ Xeon       │       │        │ (SSD 1024  │       │ A2000 (6   │      │
                     │       │ Silver     │       │        │ / HDD 0)   │       │ GB VRAM)   │      │
                     │       │ 4310       │       │        │            │       │            │      │
                     ╰───────┴────────────┴───────┴────────┴────────────┴───────┴────────────┴──────╯
                     
                     """, output);
                     

        // Describe (strict)
        (output, yaml) = await ExecuteAsync("servers", "describe", "srv01");
        Assert.Equal("""
                    ╭─Server───────────────────────────────╮
                    │ Name  srv01                          │
                    │ IPMI  yes                            │
                    │ RAM   128 GB                         │
                    │ CPU   Intel Xeon Silver 4310 (12/24) │
                    ╰──────────────────────────────────────╯

                    """, output);
                     

        // Tree (loose)
        (output, yaml) = await ExecuteAsync("servers", "tree", "srv01");
        Assert.Contains("srv01", output);
        

        // Delete server
        (output, yaml) = await ExecuteAsync("servers", "del", "srv01");
        Assert.Equal("""
                     Server 'srv01' deleted.

                     """, output);
    }
}
