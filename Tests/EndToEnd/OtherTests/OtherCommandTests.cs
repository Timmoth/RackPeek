using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.OtherTests;

[Collection("Process-wide static state")]
public class OtherCommandTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
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
        await ExecuteAsync("other", "add", "radio01");
        await ExecuteAsync("other", "set", "radio01", "--model", "Building Bridge XG", "--description", "Microwave radio bridge");

        // when
        (var output, var _) = await ExecuteAsync("other", "describe", "radio01");

        // then
        Assert.Contains("Name:", output);
        Assert.Contains("radio01", output);

        Assert.Contains("Model:", output);
        Assert.Contains("Building Bridge XG", output);

        Assert.Contains("Description:", output);
        Assert.Contains("Microwave radio bridge", output);
    }

    [Fact]
    public async Task help_outputs_do_not_throw() {
        (var rootHelp, var _) = await ExecuteAsync("other", "--help");
        Assert.Contains("Manage other hardware", rootHelp);

        (var addHelp, var _) = await ExecuteAsync("other", "add", "--help");
        Assert.Contains("Add new other hardware", addHelp);

        (var setHelp, var _) = await ExecuteAsync("other", "set", "--help");
        Assert.Contains("Update properties", setHelp);

        (var describeHelp, var _) = await ExecuteAsync("other", "describe", "--help");
        Assert.Contains("Show detailed information", describeHelp);

        (var delHelp, var _) = await ExecuteAsync("other", "del", "--help");
        Assert.Contains("Delete other hardware", delHelp);
        (var renameHelp, var _) = await ExecuteAsync("other", "rename", "--help");
        Assert.Contains("Rename other hardware", renameHelp);

        // Port help
        (var portHelp, var _) = await ExecuteAsync("other", "port", "--help");
        Assert.Contains("Manage ports on other hardware", portHelp);

        (var portAddHelp, var _) = await ExecuteAsync("other", "port", "add", "--help");
        Assert.Contains("Add a port to other hardware", portAddHelp);

        (var portSetHelp, var _) = await ExecuteAsync("other", "port", "set", "--help");
        Assert.Contains("Update an other hardware port", portSetHelp);

        (var portDelHelp, var _) = await ExecuteAsync("other", "port", "del", "--help");
        Assert.Contains("Remove a port from other hardware", portDelHelp);
    }

    [Fact]
    public async Task rename_successfully_updates_name() {
        await ExecuteAsync("other", "add", "radio01");

        (var output, var yaml) = await ExecuteAsync("other", "rename", "radio01", "radio01-new");

        Assert.Equal("Other hardware 'radio01' renamed to 'radio01-new'.\n", output);
        Assert.Contains("name: radio01-new", yaml);
    }
}
