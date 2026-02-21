using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd;

[Collection("Yaml CLI tests")]
public class DesktopWorkflowTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture>
{
    private async Task<(string, string)> ExecuteAsync(params string[] args)
    {
        outputHelper.WriteLine($"rpk {string.Join(" ", args)}");

        var output = await YamlCliTestHost.RunAsync(
            args,
            fs.Root,
            outputHelper,
            "config.yaml"
        );

        outputHelper.WriteLine(output);

        var yaml = await File.ReadAllTextAsync(Path.Combine(fs.Root, "config.yaml"));
        return (output, yaml);
    }

    [Fact]
    public async Task desktops_cli_workflow_test()
    {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Add desktop
        var (output, yaml) = await ExecuteAsync("desktops", "add", "workstation01");
        Assert.Equal("Desktop 'workstation01' added.\n", output);
        Assert.Contains("name: workstation01", yaml);

        // Update desktop
        (output, yaml) = await ExecuteAsync(
            "desktops", "set", "workstation01",
            "--model", "Dell Precision 7960"
        );
        Assert.Equal("Desktop 'workstation01' updated.\n", output);
        Assert.Contains("model: Dell Precision 7960", yaml);

        // Add CPU
        (output, yaml) = await ExecuteAsync(
            "desktops", "cpu", "add", "workstation01",
            "--model", "Intel Xeon W7-2495X",
            "--cores", "24",
            "--threads", "48"
        );
        Assert.Equal("CPU added to desktop 'workstation01'.\n", output);

        // Add Drive
        (output, yaml) = await ExecuteAsync(
            "desktops", "drive", "add", "workstation01",
            "--type", "ssd",
            "--size", "2000"
        );
        Assert.Equal("Drive added to desktop 'workstation01'.\n", output);

        // Add GPU
        (output, yaml) = await ExecuteAsync(
            "desktops", "gpu", "add", "workstation01",
            "--model", "NVIDIA RTX 4090",
            "--vram", "24"
        );
        Assert.Equal("GPU added to desktop 'workstation01'.\n", output);

        // Add NIC
        (output, yaml) = await ExecuteAsync(
            "desktops", "nic", "add", "workstation01",
            "--type", "rj45",
            "--speed", "10",
            "--ports", "2"
        );
        Assert.Equal("NIC added to desktop 'workstation01'.\n", output);

        // List desktops
        (output, yaml) = await ExecuteAsync("desktops", "list");
        Assert.Contains("workstation01", output);

        // Summary
        (output, yaml) = await ExecuteAsync("desktops", "summary");

        // Describe
        (output, yaml) = await ExecuteAsync("desktops", "describe", "workstation01");

        // Identity
        Assert.Contains("Desktop", output);
        Assert.Contains("workstation01", output);

        // Model
        Assert.Contains("Dell Precision 7960", output);

        // CPU summary
        Assert.Contains("CPUs:", output);
        Assert.Contains("1", output);
        
        // RAM summary
        Assert.Contains("RAM:", output);
        Assert.Contains("1", output);

        // Drive summary
        Assert.Contains("Drives:", output);
        Assert.Contains("1", output);

        // NIC summary
        Assert.Contains("NICs:", output);
        Assert.Contains("1", output);

        // GPU summary
        Assert.Contains("GPUs:", output);
        Assert.Contains("1", output);

        // ToDo Tree command not currently working as intended
        
        // Tree 
       // (output, yaml) = await ExecuteAsync("desktops", "tree", "workstation01");
       // Assert.Contains("workstation01", output);
       // Assert.Contains("CPU:", output);
       // Assert.Contains("RAM:", output);
       // Assert.Contains("Drive:", output);
       // Assert.Contains("GPU:", output);
       // Assert.Contains("NIC:", output);

        // Delete desktop
        (output, yaml) = await ExecuteAsync("desktops", "del", "workstation01");
        Assert.Equal("""
                     Desktop 'workstation01' deleted.

                     """, output);
    }

    [Fact]
    public async Task desktops_tree_cli_workflow_test()
    {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Add desktop
        var (output, yaml) = await ExecuteAsync("desktops", "add", "workstation01");
        Assert.Equal("Desktop 'workstation01' added.\n", output);

        // Add systems
        (output, yaml) = await ExecuteAsync("systems", "add", "sys01");
        Assert.Equal("System 'sys01' added.\n", output);

        (output, yaml) = await ExecuteAsync("systems", "add", "sys02");
        Assert.Equal("System 'sys02' added.\n", output);

        (output, yaml) = await ExecuteAsync("systems", "add", "sys03");
        Assert.Equal("System 'sys03' added.\n", output);

        // Attach systems
        await ExecuteAsync("systems", "set", "sys01", "--runs-on", "workstation01");
        await ExecuteAsync("systems", "set", "sys02", "--runs-on", "workstation01");
        await ExecuteAsync("systems", "set", "sys03", "--runs-on", "workstation01");

        // Add services
        await ExecuteAsync("services", "add", "immich");
        await ExecuteAsync("services", "add", "paperless");

        // Attach services
        await ExecuteAsync("services", "set", "immich", "--runs-on", "sys01");
        await ExecuteAsync("services", "set", "paperless", "--runs-on", "sys01");

        // Render tree
        (output, yaml) = await ExecuteAsync("desktops", "tree", "workstation01");
        Assert.Equal("""
                     workstation01
                     ├── System: sys01
                     │   ├── Service: immich
                     │   └── Service: paperless
                     ├── System: sys02
                     └── System: sys03

                     """, output);
    }
}