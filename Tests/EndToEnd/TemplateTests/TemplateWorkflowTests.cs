using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.TemplateTests;

[Collection("Yaml CLI tests")]
public class TemplateWorkflowTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture>
{
    private async Task<(string Output, string Yaml)> ExecuteAsync(params string[] args)
    {
        outputHelper.WriteLine($"rpk {string.Join(" ", args)}");

        var output = await YamlCliTestHost.RunAsync(
            args,
            fs.Root,
            outputHelper,
            "config.yaml"
        );

        outputHelper.WriteLine(output);

        var yaml = await File.ReadAllTextAsync(Path.Combine(fs.Root, "config.yaml"));
        return (output, yaml);
    }

    [Fact]
    public async Task template_list__returns_bundled_templates()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Act
        var (output, _) = await ExecuteAsync("templates", "list");

        // Assert — should contain at least some known templates
        Assert.Contains("Switch", output);
        Assert.Contains("UniFi-USW-Enterprise-24", output);
        Assert.Contains("Router", output);
        Assert.Contains("Firewall", output);
    }

    [Fact]
    public async Task template_list__filter_by_kind__returns_only_matching()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Act
        var (output, _) = await ExecuteAsync("templates", "list", "--kind", "Switch");

        // Assert
        Assert.Contains("Switch", output);
        Assert.DoesNotContain("Router", output);
        Assert.DoesNotContain("Firewall", output);
    }

    [Fact]
    public async Task template_show__existing_template__shows_details()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Act
        var (output, _) = await ExecuteAsync("templates", "show", "Switch/UniFi-USW-Enterprise-24");

        // Assert
        Assert.Contains("Switch/UniFi-USW-Enterprise-24", output);
        Assert.Contains("Switch", output);
        Assert.Contains("UniFi-USW-Enterprise-24", output);
    }

    [Fact]
    public async Task template_show__nonexistent_template__returns_error()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Act
        var (output, _) = await ExecuteAsync("templates", "show", "Switch/DoesNotExist");

        // Assert
        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task switch_add_with_template__creates_prefilled_switch()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Act
        var (output, yaml) = await ExecuteAsync(
            "switches", "add", "core-switch", "--template", "UniFi-USW-Enterprise-24");

        // Assert
        Assert.Equal("Switch 'core-switch' added.\n", output);
        Assert.Contains("name: core-switch", yaml);
        Assert.Contains("model: UniFi-USW-Enterprise-24", yaml);
        Assert.Contains("managed: true", yaml);
        Assert.Contains("poe: true", yaml);
    }

    [Fact]
    public async Task router_add_with_template__creates_prefilled_router()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Act
        var (output, yaml) = await ExecuteAsync(
            "routers", "add", "edge-router", "--template", "Ubiquiti-ER-4");

        // Assert
        Assert.Equal("Router 'edge-router' added.\n", output);
        Assert.Contains("name: edge-router", yaml);
        Assert.Contains("model: Ubiquiti-ER-4", yaml);
        Assert.Contains("managed: true", yaml);
    }

    [Fact]
    public async Task firewall_add_with_template__creates_prefilled_firewall()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Act
        var (output, yaml) = await ExecuteAsync(
            "firewalls", "add", "main-fw", "--template", "Netgate-6100");

        // Assert
        Assert.Equal("Firewall 'main-fw' added.\n", output);
        Assert.Contains("name: main-fw", yaml);
        Assert.Contains("model: Netgate-6100", yaml);
    }

    [Fact]
    public async Task switch_add_with_template__describe_shows_ports()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Act — create with template then describe
        await ExecuteAsync("switches", "add", "test-sw", "--template", "UniFi-USW-16-PoE");
        var (output, _) = await ExecuteAsync("switches", "describe", "test-sw");

        // Assert — describe output should contain the template's port data
        Assert.Contains("test-sw", output);
        Assert.Contains("UniFi-USW-16-PoE", output);
    }

    [Fact]
    public async Task switch_add_without_template__creates_blank()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Act
        var (output, yaml) = await ExecuteAsync("switches", "add", "blank-switch");

        // Assert
        Assert.Equal("Switch 'blank-switch' added.\n", output);
        Assert.Contains("name: blank-switch", yaml);
        Assert.DoesNotContain("model:", yaml);
    }

    [Fact]
    public async Task switch_add_with_invalid_template__returns_error()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Act
        var (output, _) = await ExecuteAsync(
            "switches", "add", "bad-switch", "--template", "NonExistentModel");

        // Assert
        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task switch_add_with_template__duplicate_name__returns_error()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");
        await ExecuteAsync("switches", "add", "dupe-switch");

        // Act
        var (output, _) = await ExecuteAsync(
            "switches", "add", "dupe-switch", "--template", "UniFi-USW-Enterprise-24");

        // Assert
        Assert.Contains("already exists", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task server_add_with_template__creates_prefilled_server()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Act
        var (output, yaml) = await ExecuteAsync(
            "servers", "add", "home-nuc", "--template", "Intel-NUC-13-Pro");

        // Assert
        Assert.Equal("Server 'home-nuc' added.\n", output);
        Assert.Contains("name: home-nuc", yaml);
        Assert.Contains("Intel Core i7-1360P", yaml);
        Assert.Contains("size: 32", yaml);
        Assert.Contains("type: nvme", yaml);
        Assert.Contains("ipmi: false", yaml);
    }

    [Fact]
    public async Task template_list__includes_server_templates()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Act
        var (output, _) = await ExecuteAsync("templates", "list");

        // Assert
        Assert.Contains("Server", output);
        Assert.Contains("Intel-NUC-13-Pro", output);
    }

    [Fact]
    public async Task template_show__server_template__shows_details()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Act
        var (output, _) = await ExecuteAsync("templates", "show", "Server/Intel-NUC-13-Pro");

        // Assert
        Assert.Contains("Server/Intel-NUC-13-Pro", output);
        Assert.Contains("Server", output);
        Assert.Contains("Intel-NUC-13-Pro", output);
        Assert.Contains("IPMI", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("RAM", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CPU", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task template_validate__valid_server__passes()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");
        var templatePath = Path.Combine(fs.Root, "valid-server.yaml");
        await File.WriteAllTextAsync(templatePath, """
            kind: Server
            name: test-server
            cpus:
              - model: Intel N100
                cores: 4
                threads: 4
            ram:
              size: 16
              mts: 3200
            drives:
              - type: nvme
                size: 512
            nics:
              - type: rj45
                speed: 1
                ports: 1
            ipmi: false
            """);

        // Act
        var (output, _) = await ExecuteAsync("templates", "validate", templatePath);

        // Assert
        Assert.Contains("Valid", output);
    }

    [Fact]
    public async Task template_validate__valid_switch__passes()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");
        var templatePath = Path.Combine(fs.Root, "valid-switch.yaml");
        await File.WriteAllTextAsync(templatePath, """
            kind: Switch
            name: test-switch
            model: Test-24
            managed: true
            poe: true
            ports:
              - type: rj45
                speed: 1
                count: 24
            """);

        // Act
        var (output, _) = await ExecuteAsync("templates", "validate", templatePath);

        // Assert
        Assert.Contains("Valid", output);
    }

    [Fact]
    public async Task template_validate__invalid_kind__reports_error()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");
        var templatePath = Path.Combine(fs.Root, "bad-kind.yaml");
        await File.WriteAllTextAsync(templatePath, """
            kind: Toaster
            name: not-real
            """);

        // Act
        var (output, _) = await ExecuteAsync("templates", "validate", templatePath);

        // Assert
        Assert.Contains("Invalid", output);
        Assert.Contains("kind", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task template_validate__invalid_drive_type__reports_error()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");
        var templatePath = Path.Combine(fs.Root, "bad-drive.yaml");
        await File.WriteAllTextAsync(templatePath, """
            kind: Server
            name: bad-drive
            drives:
              - type: floppy
                size: 1
            """);

        // Act
        var (output, _) = await ExecuteAsync("templates", "validate", templatePath);

        // Assert
        Assert.Contains("Invalid", output);
        Assert.Contains("floppy", output);
    }

    [Fact]
    public async Task template_validate__missing_file__reports_error()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // Act
        var (output, _) = await ExecuteAsync(
            "templates", "validate", Path.Combine(fs.Root, "nonexistent.yaml"));

        // Assert
        Assert.Contains("not found", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task template_validate__switch_missing_model__reports_error()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");
        var templatePath = Path.Combine(fs.Root, "no-model.yaml");
        await File.WriteAllTextAsync(templatePath, """
            kind: Switch
            name: no-model
            ports:
              - type: rj45
                speed: 1
                count: 8
            """);

        // Act
        var (output, _) = await ExecuteAsync("templates", "validate", templatePath);

        // Assert
        Assert.Contains("Invalid", output);
        Assert.Contains("model", output, StringComparison.OrdinalIgnoreCase);
    }
}
