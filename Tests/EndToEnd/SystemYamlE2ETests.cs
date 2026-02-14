using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd;

[Collection("Yaml CLI tests")]
public class SystemYamlE2ETests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture>
{
    private async Task<(string, string)> ExecuteAsync(params string[] args)
    {
        outputHelper.WriteLine($"rpk {string.Join(" ", args)}");

        var inputArgs = args.ToArray();
        var output = await YamlCliTestHost.RunAsync(
            inputArgs,
            fs.Root,
            outputHelper,
            "config.yaml");

        outputHelper.WriteLine(output);

        var yaml = await File.ReadAllTextAsync(Path.Combine(fs.Root, "config.yaml"));
        return (output, yaml);
    }

    [Fact]
    public async Task systems_cli_workflow_test()
    {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        var (output, yaml) = await ExecuteAsync("servers", "add", "hypervisor01");
        (output, yaml) = await ExecuteAsync("systems", "add", "host01");
        Assert.Equal("System 'host01' added.\n", output);
        Assert.Equal("""
                     resources:
                     - kind: Server
                       name: hypervisor01
                     - kind: System
                       name: host01

                     """, yaml);

        // Update system
        (output, yaml) = await ExecuteAsync(
            "systems", "set", "host01",
            "--type", "baremetal",
            "--os", "ubuntu-22.04",
            "--cores", "4",
            "--ram", "8192",
            "--runs-on", "hypervisor01"
        );

        Assert.Equal("System 'host01' updated.\n", output);

        Assert.Equal("""
                     resources:
                     - kind: Server
                       name: hypervisor01
                     - kind: System
                       type: baremetal
                       os: ubuntu-22.04
                       cores: 4
                       ram: 8192
                       runsOn: hypervisor01
                       name: host01

                     """, yaml);

        // Get system by name
        (output, yaml) = await ExecuteAsync("systems", "get", "host01");
        Assert.Equal(
            "host01  Type: baremetal, OS: ubuntu-22.04, Cores: 4, RAM: 8192GB, Storage: 0GB, \nRunsOn: hypervisor01\n",
            output);

        // List systems
        (output, yaml) = await ExecuteAsync("systems", "list");
        Assert.Equal("""
                     ╭────────┬───────────┬────────────┬───────┬──────────┬────────────┬────────────╮
                     │ Name   │ Type      │ OS         │ Cores │ RAM (GB) │ Storage    │ Runs On    │
                     │        │           │            │       │          │ (GB)       │            │
                     ├────────┼───────────┼────────────┼───────┼──────────┼────────────┼────────────┤
                     │ host01 │ baremetal │ ubuntu-22. │ 4     │ 8192     │ 0          │ hypervisor │
                     │        │           │ 04         │       │          │            │ 01         │
                     ╰────────┴───────────┴────────────┴───────┴──────────┴────────────┴────────────╯

                     """, output);

        // Report systems
        (output, yaml) = await ExecuteAsync("systems", "summary");
        Assert.Equal("""
                     ╭────────┬───────────┬────────────┬───────┬──────────┬────────────┬────────────╮
                     │ Name   │ Type      │ OS         │ Cores │ RAM (GB) │ Storage    │ Runs On    │
                     │        │           │            │       │          │ (GB)       │            │
                     ├────────┼───────────┼────────────┼───────┼──────────┼────────────┼────────────┤
                     │ host01 │ baremetal │ ubuntu-22. │ 4     │ 8192     │ 0          │ hypervisor │
                     │        │           │ 04         │       │          │            │ 01         │
                     ╰────────┴───────────┴────────────┴───────┴──────────┴────────────┴────────────╯

                     """, output);

        // Delete system
        (output, yaml) = await ExecuteAsync("systems", "del", "host01");
        Assert.Equal("""
                     System 'host01' deleted.

                     """, output);

        // Ensure list is empty
        (output, yaml) = await ExecuteAsync("systems", "list");
        Assert.Equal("""
                     No systems found.

                     """, output);
    }

    [Fact]
    public async Task system_tree_cli_workflow_test()
    {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        var (output, yaml) = await ExecuteAsync("systems", "add", "host01");
        Assert.Equal("System 'host01' added.\n", output);

        (output, yaml) = await ExecuteAsync("services", "add", "immich");
        Assert.Equal("Service 'immich' added.\n", output);

        (output, yaml) = await ExecuteAsync("services", "add", "paperless");
        Assert.Equal("Service 'paperless' added.\n", output);

        (output, yaml) = await ExecuteAsync(
            "services", "set", "immich",
            "--runs-on", "host01"
        );

        (output, yaml) = await ExecuteAsync(
            "services", "set", "paperless",
            "--runs-on", "host01"
        );

        (output, yaml) = await ExecuteAsync("systems", "tree", "host01");
        Assert.Equal("""
                     host01
                     ├── Service: immich
                     └── Service: paperless

                     """, output);
    }
}