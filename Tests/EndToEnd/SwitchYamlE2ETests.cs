using Xunit.Abstractions;

namespace Tests.EndToEnd;

public class SwitchYamlE2ETests(TempYamlCliFixture fs, ITestOutputHelper outputHelper) : IClassFixture<TempYamlCliFixture>
{
    [Fact]
    public async Task switches_add_writes_to_yaml_file()
    {
        // Act
        var output = await YamlCliTestHost.RunAsync(
            new[] { "switches", "add", "sw01" },
            fs.Root,
            outputHelper
        );

        outputHelper.WriteLine(output);
        
        // Assert
        var yaml = await File.ReadAllTextAsync(Path.Combine(fs.Root, "servers.yaml"));
        Assert.Contains("name: sw01", yaml);
    }
}