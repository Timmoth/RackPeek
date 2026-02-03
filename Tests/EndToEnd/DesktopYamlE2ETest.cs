using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd;

[Collection("Yaml CLI tests")]
public class DesktopYamlE2ETests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
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

        var (output, yaml) = await ExecuteAsync("desktops", "add", "workstation01");
        Assert.Equal("Desktop 'workstation01' added.\n", output);
        Assert.Contains("name: workstation01", yaml);
    }

    [Fact]
    public async Task desktops_tree_cli_workflow_test()
    {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Add desktop
        var (output, yaml) = await ExecuteAsync("desktops", "add", "workstation01");
        Assert.Equal("Desktop 'workstation01' added.\n", output);
        Assert.Contains("name: workstation01", yaml);

        // Add systems
        (output, yaml) = await ExecuteAsync("systems", "add", "sys01");
        Assert.Equal("System 'sys01' added.\n", output);

        (output, yaml) = await ExecuteAsync("systems", "add", "sys02");
        Assert.Equal("System 'sys02' added.\n", output);

        (output, yaml) = await ExecuteAsync("systems", "add", "sys03");
        Assert.Equal("System 'sys03' added.\n", output);

        // Attach systems to desktop
        (output, yaml) = await ExecuteAsync("systems", "set", "sys01", "--runs-on", "workstation01");
        Assert.Equal("System 'sys01' updated.\n", output);

        (output, yaml) = await ExecuteAsync("systems", "set", "sys02", "--runs-on", "workstation01");
        Assert.Equal("System 'sys02' updated.\n", output);

        (output, yaml) = await ExecuteAsync("systems", "set", "sys03", "--runs-on", "workstation01");
        Assert.Equal("System 'sys03' updated.\n", output);

        // Add services
        (output, yaml) = await ExecuteAsync("services", "add", "immich");
        Assert.Equal("Service 'immich' added.\n", output);

        (output, yaml) = await ExecuteAsync("services", "add", "paperless");
        Assert.Equal("Service 'paperless' added.\n", output);

        // Attach services to sys01
        (output, yaml) = await ExecuteAsync("services", "set", "immich", "--runs-on", "sys01");
        Assert.Equal("Service 'immich' updated.\n", output);

        (output, yaml) = await ExecuteAsync("services", "set", "paperless", "--runs-on", "sys01");
        Assert.Equal("Service 'paperless' updated.\n", output);

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