using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.LaptopTests;

[Collection("Process-wide static state")]
public class LaptopNicWorkflowTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture> {
    private async Task<(string, string)> ExecuteAsync(params string[] args) {
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
    public async Task laptop_nic_cli_workflow_test() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        await ExecuteAsync("laptops", "add", "lap01");
        await ExecuteAsync("laptops", "set", "lap01", "--model", "ThinkPad X1 Carbon");

        // Built-in wired NIC.
        (var output, var yaml) = await ExecuteAsync(
            "laptops", "nic", "add", "lap01",
            "--type", "rj45",
            "--speed", "1",
            "--ports", "1"
        );
        Assert.Equal("NIC added to Laptop 'lap01'.\n", output);

        // Dock, attached over USB.
        (output, yaml) = await ExecuteAsync(
            "laptops", "nic", "add", "lap01",
            "--type", "usb",
            "--speed", "10",
            "--ports", "2"
        );
        Assert.Equal("NIC added to Laptop 'lap01'.\n", output);

        Assert.Equal("""
                     version: 4
                     resources:
                     - kind: Laptop
                       model: ThinkPad X1 Carbon
                       ports:
                       - type: rj45
                         speed: 1
                         count: 1
                       - type: usb
                         speed: 10
                         count: 2
                       name: lap01
                     connections: []

                     """, yaml);

        (output, yaml) = await ExecuteAsync(
            "laptops", "nic", "set", "lap01", "1",
            "--type", "usb",
            "--speed", "20",
            "--ports", "2"
        );
        Assert.Equal("NIC #1 updated on Laptop 'lap01'.\n", output);
        Assert.Contains("speed: 20", yaml);

        // Describe reports the NIC count, matching how desktops report theirs.
        (output, yaml) = await ExecuteAsync("laptops", "describe", "lap01");
        Assert.Contains("NICs:", output);
        Assert.Contains("2", output);

        (output, yaml) = await ExecuteAsync("laptops", "nic", "del", "lap01", "1");
        Assert.Equal("NIC #1 removed from Laptop 'lap01'.\n", output);

        Assert.Equal("""
                     version: 4
                     resources:
                     - kind: Laptop
                       model: ThinkPad X1 Carbon
                       ports:
                       - type: rj45
                         speed: 1
                         count: 1
                       name: lap01
                     connections: []

                     """, yaml);
    }

    [Fact]
    public async Task describe_reports_zero_nics_when_laptop_has_none() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        await ExecuteAsync("laptops", "add", "lap-bare");

        (var output, var _) = await ExecuteAsync("laptops", "describe", "lap-bare");

        Assert.Contains("NICs:", output);
        Assert.Contains("0", output);
    }
}
