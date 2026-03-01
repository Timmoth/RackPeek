using RackPeek.Domain.Templates;

namespace Tests.Unit.Templates;

public class TemplateValidatorTests
{
    private readonly TemplateValidator _sut = new();

    [Fact]
    public void validate__valid_server__returns_no_errors()
    {
        var yaml = """
            kind: Server
            name: test-server
            cpus:
              - model: Intel Core i7-1360P
                cores: 12
                threads: 16
            ram:
              size: 32
              mts: 4800
            drives:
              - type: nvme
                size: 1024
            nics:
              - type: rj45
                speed: 2.5
                ports: 1
            ipmi: false
            """;

        var errors = _sut.Validate(yaml, "test-server.yaml");

        Assert.Empty(errors);
    }

    [Fact]
    public void validate__valid_switch__returns_no_errors()
    {
        var yaml = """
            kind: Switch
            name: test-switch
            model: USW-Enterprise-24
            managed: true
            poe: true
            ports:
              - type: rj45
                speed: 1
                count: 24
            """;

        var errors = _sut.Validate(yaml, "test-switch.yaml");

        Assert.Empty(errors);
    }

    [Fact]
    public void validate__valid_router__returns_no_errors()
    {
        var yaml = """
            kind: Router
            name: test-router
            model: ER-4
            ports:
              - type: rj45
                speed: 1
                count: 4
            managed: true
            poe: false
            """;

        var errors = _sut.Validate(yaml, "test-router.yaml");

        Assert.Empty(errors);
    }

    [Fact]
    public void validate__valid_firewall__returns_no_errors()
    {
        var yaml = """
            kind: Firewall
            name: test-fw
            model: Netgate-6100
            ports:
              - type: sfp+
                speed: 10
                count: 2
            managed: true
            poe: false
            """;

        var errors = _sut.Validate(yaml, "test-fw.yaml");

        Assert.Empty(errors);
    }

    [Fact]
    public void validate__valid_accesspoint__returns_no_errors()
    {
        var yaml = """
            kind: AccessPoint
            name: test-ap
            model: U6-Pro
            speed: 2.5
            """;

        var errors = _sut.Validate(yaml, "test-ap.yaml");

        Assert.Empty(errors);
    }

    [Fact]
    public void validate__valid_ups__returns_no_errors()
    {
        var yaml = """
            kind: Ups
            name: test-ups
            model: APC-2200
            va: 2200
            """;

        var errors = _sut.Validate(yaml, "test-ups.yaml");

        Assert.Empty(errors);
    }

    [Fact]
    public void validate__empty_file__returns_error()
    {
        var errors = _sut.Validate("", "empty.yaml");

        Assert.Single(errors);
        Assert.Contains("empty", errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void validate__invalid_yaml__returns_parse_error()
    {
        var errors = _sut.Validate("this is: [not valid: {{}}", "bad.yaml");

        Assert.Single(errors);
        Assert.Contains("YAML parsing failed", errors[0]);
    }

    [Fact]
    public void validate__missing_kind__returns_error()
    {
        var yaml = """
            name: no-kind
            model: Something
            """;

        var errors = _sut.Validate(yaml, "no-kind.yaml");

        Assert.Contains(errors, e => e.Contains("kind", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void validate__invalid_kind__returns_error()
    {
        var yaml = """
            kind: Toaster
            name: not-real
            """;

        var errors = _sut.Validate(yaml, "toaster.yaml");

        Assert.Single(errors);
        Assert.Contains("Invalid kind", errors[0]);
    }

    [Fact]
    public void validate__missing_name__returns_error()
    {
        var yaml = """
            kind: Switch
            model: Test-Switch
            ports:
              - type: rj45
                speed: 1
                count: 8
            """;

        var errors = _sut.Validate(yaml, "no-name.yaml");

        Assert.Contains(errors, e => e.Contains("name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void validate__switch_missing_model__returns_error()
    {
        var yaml = """
            kind: Switch
            name: no-model
            ports:
              - type: rj45
                speed: 1
                count: 8
            """;

        var errors = _sut.Validate(yaml, "no-model.yaml");

        Assert.Contains(errors, e => e.Contains("model", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void validate__server_invalid_drive_type__returns_error()
    {
        var yaml = """
            kind: Server
            name: bad-drive
            drives:
              - type: floppy
                size: 1
            """;

        var errors = _sut.Validate(yaml, "bad-drive.yaml");

        Assert.Contains(errors, e => e.Contains("floppy") && e.Contains("invalid type"));
    }

    [Fact]
    public void validate__server_invalid_nic_type__returns_error()
    {
        var yaml = """
            kind: Server
            name: bad-nic
            nics:
              - type: coax
                speed: 10
                ports: 1
            """;

        var errors = _sut.Validate(yaml, "bad-nic.yaml");

        Assert.Contains(errors, e => e.Contains("coax") && e.Contains("invalid type"));
    }

    [Fact]
    public void validate__switch_invalid_port_type__returns_error()
    {
        var yaml = """
            kind: Switch
            name: bad-port
            model: Bad-Switch
            ports:
              - type: coax
                speed: 1
                count: 4
            """;

        var errors = _sut.Validate(yaml, "bad-port.yaml");

        Assert.Contains(errors, e => e.Contains("coax") && e.Contains("invalid type"));
    }

    [Fact]
    public void validate__server_cpu_missing_model__returns_error()
    {
        var yaml = """
            kind: Server
            name: no-cpu-model
            cpus:
              - cores: 4
                threads: 8
            """;

        var errors = _sut.Validate(yaml, "no-cpu-model.yaml");

        Assert.Contains(errors, e => e.Contains("cpus[0]") && e.Contains("model"));
    }

    [Fact]
    public void validate__server_gpu_missing_model__returns_error()
    {
        var yaml = """
            kind: Server
            name: no-gpu-model
            gpus:
              - vram: 8
            """;

        var errors = _sut.Validate(yaml, "no-gpu-model.yaml");

        Assert.Contains(errors, e => e.Contains("gpus[0]") && e.Contains("model"));
    }

    [Fact]
    public void validate__ups_missing_model__returns_error()
    {
        var yaml = """
            kind: Ups
            name: no-model-ups
            va: 1500
            """;

        var errors = _sut.Validate(yaml, "no-model-ups.yaml");

        Assert.Contains(errors, e => e.Contains("model", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void validate__accesspoint_missing_model__returns_error()
    {
        var yaml = """
            kind: AccessPoint
            name: no-model-ap
            speed: 1
            """;

        var errors = _sut.Validate(yaml, "no-model-ap.yaml");

        Assert.Contains(errors, e => e.Contains("model", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void validate__multiple_errors__returns_all()
    {
        var yaml = """
            kind: Server
            name: multi-error
            cpus:
              - cores: 4
            drives:
              - type: floppy
                size: 1
            nics:
              - type: coax
                speed: 1
                ports: 1
            gpus:
              - vram: 4
            """;

        var errors = _sut.Validate(yaml, "multi.yaml");

        Assert.True(errors.Count >= 4);
        Assert.Contains(errors, e => e.Contains("cpus[0]"));
        Assert.Contains(errors, e => e.Contains("drives[0]"));
        Assert.Contains(errors, e => e.Contains("nics[0]"));
        Assert.Contains(errors, e => e.Contains("gpus[0]"));
    }
}
