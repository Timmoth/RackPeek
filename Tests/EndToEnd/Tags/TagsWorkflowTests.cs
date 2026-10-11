using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.Tags;

[Collection("Process-wide static state")]
public class TagsWorkflowTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
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

    [Theory]
    [InlineData("servers")]
    [InlineData("switches")]
    [InlineData("routers")]
    [InlineData("firewalls")]
    [InlineData("accesspoints")]
    [InlineData("ups")]
    [InlineData("desktops")]
    [InlineData("laptops")]
    [InlineData("services")]
    [InlineData("systems")]
    public async Task tags_cli_workflow_test(string resourceCommand) {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        (_, var yaml) = await ExecuteAsync(resourceCommand, "add", "web-01");
        Assert.Contains("web-01", yaml);

        (var output, yaml) = await ExecuteAsync(resourceCommand, "tag", "add", "web-01", "homelab");
        Assert.Contains("Tag 'homelab' added", output);
        Assert.Contains("homelab", yaml);

        (output, _) = await ExecuteAsync("tags", "show", "homelab");
        Assert.Contains("web-01", output);

        (output, yaml) = await ExecuteAsync(resourceCommand, "tag", "remove", "web-01", "homelab");
        Assert.Contains("Tag 'homelab' removed", output);
        Assert.DoesNotContain("homelab", yaml);
    }

    [Fact]
    public async Task tags_discovery_test() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        await ExecuteAsync("servers", "add", "srv-01");
        await ExecuteAsync("services", "add", "svc-01");
        await ExecuteAsync("servers", "add", "srv-02");

        await ExecuteAsync("servers", "tag", "add", "srv-01", "homelab");
        await ExecuteAsync("services", "tag", "add", "svc-01", "homelab");
        await ExecuteAsync("servers", "tag", "add", "srv-02", "prod");

        (var output, _) = await ExecuteAsync("tags", "list");
        Assert.Contains("homelab", output);
        Assert.Contains("prod", output);

        (output, _) = await ExecuteAsync("tags", "show", "homelab");
        Assert.Contains("srv-01", output);
        Assert.Contains("svc-01", output);
        Assert.DoesNotContain("srv-02", output);
    }
}
