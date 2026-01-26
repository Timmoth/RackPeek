using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd;

[Collection("Yaml CLI tests")]
public class ServerYamlE2ETests(TempYamlCliFixture fs, ITestOutputHelper outputHelper) : IClassFixture<TempYamlCliFixture>
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
    public async Task servers_cli_workflow_test()
    {
        // Add switch
        var (output, yaml) = await ExecuteAsync("servers", "add", "node01");
        Assert.Equal("Server 'node01' added.\n", output);
        Assert.Contains("name: node01", yaml);
    }
}