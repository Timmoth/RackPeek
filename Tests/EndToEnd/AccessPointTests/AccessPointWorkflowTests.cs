using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd;

[Collection("Yaml CLI tests")]
public class AccessPointWorkflowTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture>
{
    private async Task<(string, string)> ExecuteAsync(params string[] args)
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
    public async Task accesspoints_cli_workflow_test()
    {
        var (output, yaml) = await ExecuteAsync("accesspoints", "add", "ap01");
        Assert.Equal("Access Point 'ap01' added.\n", output);
        Assert.Contains("name: ap01", yaml);

        (output, yaml) = await ExecuteAsync(
            "accesspoints", "set", "ap01",
            "--model", "Unifi-U6-Lite",
            "--speed", "1"
        );
        Assert.Equal("Access Point 'ap01' updated.\n", output);

        Assert.Equal("""
                     resources:
                     - kind: AccessPoint
                       model: Unifi-U6-Lite
                       speed: 1
                       name: ap01

                     """, yaml);

        (output, yaml) = await ExecuteAsync("accesspoints", "add", "ap02");
        Assert.Equal("Access Point 'ap02' added.\n", output);

        (output, yaml) = await ExecuteAsync(
            "accesspoints", "set", "ap02",
            "--model", "Aruba-AP-515",
            "--speed", "2.5"
        );
        Assert.Equal("Access Point 'ap02' updated.\n", output);

        Assert.Equal("""
                     resources:
                     - kind: AccessPoint
                       model: Unifi-U6-Lite
                       speed: 1
                       name: ap01
                     - kind: AccessPoint
                       model: Aruba-AP-515
                       speed: 2.5
                       name: ap02

                     """, yaml);

        (output, yaml) = await ExecuteAsync("accesspoints", "get", "ap01");
        Assert.Equal("ap01  Model: Unifi-U6-Lite, Speed: 1Gbps\n", output);

        (output, yaml) = await ExecuteAsync("accesspoints", "list");
        Assert.Equal("""
                     ╭──────┬───────────────┬──────────────╮
                     │ Name │ Model         │ Speed (Gbps) │
                     ├──────┼───────────────┼──────────────┤
                     │ ap01 │ Unifi-U6-Lite │ 1            │
                     │ ap02 │ Aruba-AP-515  │ 2.5          │
                     ╰──────┴───────────────┴──────────────╯

                     """, output);

        (output, yaml) = await ExecuteAsync("accesspoints", "summary");
        Assert.Equal("""
                     ╭──────┬───────────────┬──────────────╮
                     │ Name │ Model         │ Speed (Gbps) │
                     ├──────┼───────────────┼──────────────┤
                     │ ap01 │ Unifi-U6-Lite │ 1            │
                     │ ap02 │ Aruba-AP-515  │ 2.5          │
                     ╰──────┴───────────────┴──────────────╯

                     """, output);

        (output, yaml) = await ExecuteAsync("accesspoints", "del", "ap02");
        Assert.Equal("""
                     Access Point 'ap02' deleted.

                     """, output);

        (output, yaml) = await ExecuteAsync("accesspoints", "list");
        Assert.Equal("""
                     ╭──────┬───────────────┬──────────────╮
                     │ Name │ Model         │ Speed (Gbps) │
                     ├──────┼───────────────┼──────────────┤
                     │ ap01 │ Unifi-U6-Lite │ 1            │
                     ╰──────┴───────────────┴──────────────╯

                     """, output);
    }
}
