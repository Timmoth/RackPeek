using System.IO;
using System.Threading.Tasks;
using Tests.EndToEnd.Infra;
using Xunit;
using Xunit.Abstractions;

namespace Tests.EndToEnd.MermaidTests;

[Collection("Yaml CLI tests")]
public class MermaidExportCommandTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture>
{
    private async Task<(string output, string yaml)> ExecuteAsync(params string[] args)
    {
        outputHelper.WriteLine($"rpk {string.Join(" ", args)}");

        var output = await YamlCliTestHost.RunAsync(
            args,
            fs.Root,
            outputHelper,
            "config.yaml"
        );

        var yaml = await File.ReadAllTextAsync(Path.Combine(fs.Root, "config.yaml"));
        return (output, yaml);
    }

    [Fact]
    public async Task help_commands_do_not_throw()
    {
        (var output, var _) = await ExecuteAsync("mermaid", "export", "--help");
        Assert.Equal("""
                        DESCRIPTION:
                        Generate a Mermaid infrastructure diagram
                        
                        USAGE:
                            rpk mermaid export [OPTIONS]
                        
                        OPTIONS:
                            -h, --help               Prints help information                            
                                --include-tags       Comma-separated list of tags to include (e.g.      
                                                     prod,linux)                                        
                                --diagram-type       Mermaid diagram type (default: "flowchart TD")     
                                --no-labels          Disable resource label annotations                 
                                --no-edges           Disable relationship edges                         
                                --label-whitelist    Comma-separated list of label keys to include      
                            -o, --output             Write Mermaid diagram to file instead of stdout    
                        
                        """, output);
                       
        outputHelper.WriteLine(output);
        
        (output, _) = await ExecuteAsync("mermaid", "--help");
        Assert.Equal("""
                        DESCRIPTION:
                        Generate Mermaid diagrams from infrastructure
                        
                        USAGE:
                            rpk mermaid [OPTIONS] <COMMAND>
                        
                        OPTIONS:
                            -h, --help    Prints help information
                        
                        COMMANDS:
                            export    Generate a Mermaid infrastructure diagram
                        
                        """, output);
       
    }

    [Fact]
    public async Task mermaid_export_creates_expected_diagram()
    {
        // Prepare test resources
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Add resources
        await ExecuteAsync("servers", "add", "srv01");
        await ExecuteAsync("servers", "add", "srv02");
        await ExecuteAsync("switches", "add", "sw01");
        await ExecuteAsync("services", "add", "svc01");
        await ExecuteAsync("systems", "add", "sys01");

        // Define RunsOn relationships
        await ExecuteAsync("services", "set", "svc01", "--RunsOn", "sys01");
        await ExecuteAsync("systems", "set", "sys01", "--RunsOn", "srv01");

        // Export Mermaid
        (var output, var _) = await ExecuteAsync("mermaid", "export");

        outputHelper.WriteLine(output);

        // Assertions: check subgraphs
        Assert.Contains("subgraph servers", output);
        Assert.Contains("subgraph switches", output);
        Assert.Contains("subgraph services", output);
        Assert.Contains("subgraph systems", output);

        // Assertions: check nodes
        Assert.Contains("srv01", output);
        Assert.Contains("srv02", output);
        Assert.Contains("sw01", output);
        Assert.Contains("svc01", output);
        Assert.Contains("sys01", output);

        // Assertions: check edges
        Assert.Contains("svc01 --> sys01", output);
        Assert.Contains("sys01 --> srv01", output);

        // Ensure diagram type is included
        Assert.StartsWith("flowchart TD", output);
    }

    [Fact]
    public async Task mermaid_export_with_tags_and_labels()
    {
        // Add resources with tags and labels
        await ExecuteAsync("servers", "add", "srv03", "--Tags", "db,primary", "--Label", "env:prod");

        // Export only resources with tag 'db'
        (var output, _) = await ExecuteAsync("mermaid", "export", "--IncludeTags", "db");

        outputHelper.WriteLine(output);

        // Only srv03 should appear
        Assert.Contains("srv03", output);
        Assert.DoesNotContain("srv01", output);
        Assert.DoesNotContain("srv02", output);

        // Label should be included
        Assert.Contains("env: prod", output);
    }
}
