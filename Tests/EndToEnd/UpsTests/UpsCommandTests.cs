using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.UpsTests;

[Collection("Process-wide static state")]
public class UpsCommandTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture> {
    private async Task<(string, string)> ExecuteAsync(params string[] args) {
        var output = await YamlCliTestHost.RunAsync(
            args,
            fs.Root,
            outputHelper,
            "config.yaml");

        var yaml = await File.ReadAllTextAsync(Path.Combine(fs.Root, "config.yaml"));
        return (output, yaml);
    }

    [Fact]
    public async Task describe_returns_detailed_information() {
        // given
        await ExecuteAsync("ups", "add", "ups01");
        await ExecuteAsync("ups", "set", "ups01", "--model", "APC Smart-UPS 1500", "--va", "1500");

        // when
        (var output, var _) = await ExecuteAsync("ups", "describe", "ups01");

        // then 
        Assert.Contains("Name:", output);
        Assert.Contains("ups01", output);

        Assert.Contains("Model:", output);
        Assert.Contains("APC Smart-UPS 1500", output);

        Assert.Contains("VA", output);
        Assert.Contains("1500", output);
    }

    [Fact]
    public async Task help_outputs_do_not_throw() {
        (var rootHelp, var _) = await ExecuteAsync("ups", "--help");
        Assert.Contains("Manage UPS units", rootHelp);

        (var addHelp, var _) = await ExecuteAsync("ups", "add", "--help");
        Assert.Contains("Add a new UPS unit", addHelp);

        (var setHelp, var _) = await ExecuteAsync("ups", "set", "--help");
        Assert.Contains("Update properties", setHelp);

        (var describeHelp, var _) = await ExecuteAsync("ups", "describe", "--help");
        Assert.Contains("Show detailed information", describeHelp);

        (var delHelp, var _) = await ExecuteAsync("ups", "del", "--help");
        Assert.Contains("Delete a UPS unit", delHelp);
        (var renameHelp, var _) = await ExecuteAsync("ups", "rename", "--help");
        Assert.Contains("Rename a UPS unit", renameHelp);

        // Port help
        (var portHelp, var _) = await ExecuteAsync("ups", "port", "--help");
        Assert.Contains("Manage ports on a UPS unit", portHelp);

        (var portAddHelp, var _) = await ExecuteAsync("ups", "port", "add", "--help");
        Assert.Contains("Add a port to a UPS unit", portAddHelp);

        (var portSetHelp, var _) = await ExecuteAsync("ups", "port", "set", "--help");
        Assert.Contains("Update a UPS unit port", portSetHelp);

        (var portDelHelp, var _) = await ExecuteAsync("ups", "port", "del", "--help");
        Assert.Contains("Remove a port from a UPS unit", portDelHelp);
    }

    [Fact]
    public async Task rename_successfully_updates_name() {
        await ExecuteAsync("ups", "add", "ups01");

        (var output, var yaml) = await ExecuteAsync("ups", "rename", "ups01", "ups01-new");

        Assert.Equal("UPS 'ups01' renamed to 'ups01-new'.\n", output);
        Assert.Contains("name: ups01-new", yaml);
    }
}
