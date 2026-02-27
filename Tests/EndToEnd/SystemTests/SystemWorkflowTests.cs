using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.SystemTests;

[Collection("Yaml CLI tests")]
public class SystemWorkflowTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
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
    public async Task systems_cli_workflow_test()
    {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");
        
        await ExecuteAsync("servers", "add", "proxmox-node01");

        // Add system
        var (output, yaml) = await ExecuteAsync("systems", "add", "sys01");
        Assert.Equal("System 'sys01' added.\n", output);
        Assert.Contains("name: sys01", yaml);

        // Update system
        (output, yaml) = await ExecuteAsync(
            "systems", "set", "sys01",
            "--type", "VM",
            "--os", "debian-12",
            "--cores", "2",
            "--ram", "4",
            "--runs-on", "proxmox-node01"
        );
        Assert.Equal("System 'sys01' updated.\n", output);

        outputHelper.WriteLine(yaml);
        Assert.Equal("""
                     version: 2
                     resources:
                     - kind: Server
                       name: proxmox-node01
                     - kind: System
                       type: vm
                       os: debian-12
                       cores: 2
                       ram: 4
                       name: sys01
                       runsOn:
                       - proxmox-node01

                     """, yaml);

        // Get system
        (output, yaml) = await ExecuteAsync("systems", "get", "sys01");
        Assert.Equal("sys01  Type: vm, OS: debian-12, Cores: 2, RAM: 4GB, Storage: 0GB, RunsOn: \nproxmox-node01\n", output);

        // List systems (strict table)
        (output, yaml) = await ExecuteAsync("systems", "list");
        Assert.Equal("""
                     ╭───────┬──────┬───────────┬───────┬──────────┬──────────────┬────────────────╮
                     │ Name  │ Type │ OS        │ Cores │ RAM (GB) │ Storage (GB) │ Runs On        │
                     ├───────┼──────┼───────────┼───────┼──────────┼──────────────┼────────────────┤
                     │ sys01 │ vm   │ debian-12 │ 2     │ 4        │ 0            │ proxmox-node01 │
                     ╰───────┴──────┴───────────┴───────┴──────────┴──────────────┴────────────────╯

                     """, output);

        // Summary (strict table)
        (output, yaml) = await ExecuteAsync("systems", "summary");
        Assert.Equal("""
                     ╭───────┬──────┬───────────┬───────┬──────────┬──────────────┬────────────────╮
                     │ Name  │ Type │ OS        │ Cores │ RAM (GB) │ Storage (GB) │ Runs On        │
                     ├───────┼──────┼───────────┼───────┼──────────┼──────────────┼────────────────┤
                     │ sys01 │ vm   │ debian-12 │ 2     │ 4        │ 0            │ proxmox-node01 │
                     ╰───────┴──────┴───────────┴───────┴──────────┴──────────────┴────────────────╯

                     """, output);

        // Describe (loose)
        (output, yaml) = await ExecuteAsync("systems", "describe", "sys01");
        Assert.Contains("sys01", output);
        Assert.Contains("vm", output);
        Assert.Contains("debian-12", output);
        Assert.Contains("Cores", output);
        Assert.Contains("RAM", output);
        Assert.Contains("Runs On", output);

        // Tree (loose)
        (output, yaml) = await ExecuteAsync("systems", "tree", "sys01");
        Assert.Contains("sys01", output);
        // ToDo add a service in the workflow to properly test the tree functionality 
        //Assert.Contains("Service:", output);

        // Delete system
        (output, yaml) = await ExecuteAsync("systems", "del", "sys01");
        Assert.Equal("""
                     System 'sys01' deleted.

                     """, output);
    }
    
    [Fact]
public async Task systems_cli_workflow_runs_on_hardware_and_systems_test()
{
    await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

    // Create hardware (server)
    await ExecuteAsync("servers", "add", "proxmox-node01");

    // Add first system
    var (output, yaml) = await ExecuteAsync("systems", "add", "sys01");
    Assert.Equal("System 'sys01' added.\n", output);
    Assert.Contains("name: sys01", yaml);

    // Set sys01 to run on the created hardware
    (output, yaml) = await ExecuteAsync(
        "systems", "set", "sys01",
        "--type", "VM",
        "--os", "debian-12",
        "--cores", "2",
        "--ram", "4",
        "--runs-on", "proxmox-node01"
    );
    Assert.Equal("System 'sys01' updated.\n", output);

    // Add second system
    (output, yaml) = await ExecuteAsync("systems", "add", "sys02");
    Assert.Equal("System 'sys02' added.\n", output);
    Assert.Contains("name: sys02", yaml);

    // Set sys02 to run on BOTH: hardware + sys01
    // NOTE: '--runs-on' accepts multiple values via repeated options.
    (output, yaml) = await ExecuteAsync(
        "systems", "set", "sys02",
        "--type", "VM",
        "--os", "debian-12",
        "--cores", "4",
        "--ram", "8",
        "--runs-on", "proxmox-node01",
        "--runs-on", "sys01"
    );
    Assert.Equal("System 'sys02' updated.\n", output);

    outputHelper.WriteLine(yaml);

    // Assert resulting YAML
    Assert.Equal("""
                 version: 2
                 resources:
                 - kind: Server
                   name: proxmox-node01
                 - kind: System
                   type: vm
                   os: debian-12
                   cores: 2
                   ram: 4
                   name: sys01
                   runsOn:
                   - proxmox-node01
                 - kind: System
                   type: vm
                   os: debian-12
                   cores: 4
                   ram: 8
                   name: sys02
                   runsOn:
                   - proxmox-node01
                   - sys01

                 """, yaml);
}
}
