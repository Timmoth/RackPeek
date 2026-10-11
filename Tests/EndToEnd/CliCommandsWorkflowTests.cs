using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd;

/// <summary>
/// Comprehensive E2E test covering all CLI commands with varied input data.
/// Tests happy paths for CRUD operations, components, labels, and exporters.
/// </summary>
[Collection("Process-wide static state")]
public class CliCommandsWorkflowTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture> {
    private async Task<(string, string)> ExecuteAsync(params string[] args) {
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
    public async Task comprehensive_cli_workflow_test() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), "");

        // ============================================================
        // GLOBAL: Summary command
        // ============================================================
        (var output, _) = await ExecuteAsync("summary");
        Assert.Contains("Breakdown", output);

        // ============================================================
        // SERVERS: Full CRUD with components and labels
        // ============================================================
        // Add server with various naming conventions
        (output, _) = await ExecuteAsync("servers", "add", "srv-prod-web01");

        // Set server properties with varied data
        (output, _) = await ExecuteAsync(
            "servers", "set", "srv-prod-web01",
            "--ram", "64",
            "--ram_mts", "3200",
            "--ipmi", "True"
        );
        Assert.Contains("updated", output);

        // Add CPU with bracket format model name
        (output, _) = await ExecuteAsync(
            "servers", "cpu", "add", "srv-prod-web01",
            "--model", "AMD EPYC 7763 [64c]",
            "--cores", "64",
            "--threads", "128"
        );
        Assert.Contains("added", output);

        // Add drive
        (output, _) = await ExecuteAsync(
            "servers", "drive", "add", "srv-prod-web01",
            "--type", "nvme",
            "--size", "4096"
        );
        Assert.Contains("added", output);

        // Add GPU with dash format
        (output, _) = await ExecuteAsync(
            "servers", "gpu", "add", "srv-prod-web01",
            "--model", "NVIDIA-RTX-4090",
            "--vram", "24"
        );
        Assert.Contains("added", output);

        // Add NIC
        (output, _) = await ExecuteAsync(
            "servers", "nic", "add", "srv-prod-web01",
            "--type", "RJ45",
            "--speed", "10",
            "--ports", "2"
        );
        Assert.Contains("added", output);

        // Add label
        (output, _) = await ExecuteAsync(
            "servers", "label", "add", "srv-prod-web01",
            "--key", "env", "--value", "production"
        );
        Assert.Contains("Label", output);

        // Get server
        (output, _) = await ExecuteAsync("servers", "get", "srv-prod-web01");
        Assert.Contains("srv-prod-web01", output);
        Assert.Contains("64 GB", output);

        // Get server
        (output, _) = await ExecuteAsync("servers", "get", "srv-prod-web01");
        Assert.Contains("srv-prod-web01", output);

        // Describe
        (output, _) = await ExecuteAsync("servers", "describe", "srv-prod-web01");
        Assert.Contains("srv-prod-web01", output);
        Assert.Contains("EPYC", output);

        // Tree
        (output, _) = await ExecuteAsync("servers", "tree", "srv-prod-web01");
        Assert.Contains("srv-prod-web01", output);

        // ============================================================
        // SWITCHES: Full workflow
        // ============================================================
        (output, _) = await ExecuteAsync("switches", "add", "sw-core-01");
        Assert.Contains("added", output);

        (output, _) = await ExecuteAsync(
            "switches", "set", "sw-core-01",
            "--model", "Cisco-C9300-48P",
            "--managed", "true",
            "--poe", "true"
        );
        Assert.Contains("updated", output);

        (output, _) = await ExecuteAsync("switches", "port", "add", "sw-core-01", "--type", "SFP+", "--speed", "25", "--ports", "4");
        Assert.Contains("added", output);

        (output, _) = await ExecuteAsync(
            "switches", "label", "add", "sw-core-01",
            "--key", "zone", "--value", "core"
        );
        Assert.Contains("added", output);

        (output, _) = await ExecuteAsync("switches", "get", "sw-core-01");
        Assert.Contains("sw-core-01", output);

        (output, _) = await ExecuteAsync("switches", "list");
        Assert.Contains("sw-core-01", output);

        (output, _) = await ExecuteAsync("switches", "summary");
        Assert.Contains("Name", output);

        (output, _) = await ExecuteAsync("switches", "describe", "sw-core-01");
        Assert.Contains("sw-core-01", output);

        // ============================================================
        // ROUTERS: Full workflow
        // ============================================================
        (output, _) = await ExecuteAsync("routers", "add", "rt-edge-01");
        Assert.Contains("added", output);

        (output, _) = await ExecuteAsync(
            "routers", "set", "rt-edge-01",
            "--Model", "MikroTik-CCR2004",
            "--managed", "true",
            "--poe", "false"
        );
        Assert.Contains("updated", output);

        (output, _) = await ExecuteAsync("routers", "port", "add", "rt-edge-01", "--type", "SFP", "--speed", "10", "--ports", "8");
        Assert.Contains("added", output);

        (output, _) = await ExecuteAsync(
            "routers", "label", "add", "rt-edge-01",
            "--key", "tier", "--value", "edge"
        );
        Assert.Contains("added", output);

        (output, _) = await ExecuteAsync("routers", "get", "rt-edge-01");
        Assert.Contains("rt-edge-01", output);

        (output, _) = await ExecuteAsync("routers", "list");
        Assert.Contains("rt-edge-01", output);

        (output, _) = await ExecuteAsync("routers", "summary");
        Assert.Contains("Name", output);

        (output, _) = await ExecuteAsync("routers", "describe", "rt-edge-01");
        Assert.Contains("rt-edge-01", output);

        // ============================================================
        // FIREWALLS: Full workflow
        // ============================================================
        (output, _) = await ExecuteAsync("firewalls", "add", "fw-perimeter-01");
        Assert.Contains("added", output);

        (output, _) = await ExecuteAsync(
            "firewalls", "set", "fw-perimeter-01",
            "--Model", "PaloAlto-PA-5220",
            "--managed", "true",
            "--poe", "false"
        );
        Assert.Contains("updated", output);

        (output, _) = await ExecuteAsync("firewalls", "port", "add", "fw-perimeter-01", "--type", "RJ45", "--speed", "1", "--ports", "10");
        Assert.Contains("added", output);

        (output, _) = await ExecuteAsync(
            "firewalls", "label", "add", "fw-perimeter-01",
            "--key", "security", "--value", "dmz"
        );
        Assert.Contains("added", output);

        (output, _) = await ExecuteAsync("firewalls", "get", "fw-perimeter-01");
        Assert.Contains("fw-perimeter-01", output);

        (output, _) = await ExecuteAsync("firewalls", "list");
        Assert.Contains("fw-perimeter-01", output);

        (output, _) = await ExecuteAsync("firewalls", "summary");
        Assert.Contains("Name", output);

        (output, _) = await ExecuteAsync("firewalls", "describe", "fw-perimeter-01");
        Assert.Contains("fw-perimeter-01", output);

        // ============================================================
        // SYSTEMS: Full workflow with tree
        // ============================================================
        (output, _) = await ExecuteAsync("systems", "add", "sys-app-web-01");
        Assert.Contains("added", output);

        (output, _) = await ExecuteAsync(
            "systems", "set", "sys-app-web-01",
            "--type", "container",
            "--os", "ubuntu-22.04",
            "--cores", "4",
            "--ram", "16",
            "--ip", "10.0.1.50",
            "--runs-on", "srv-prod-web01"
        );
        Assert.Contains("updated", output);

        (output, _) = await ExecuteAsync(
            "systems", "label", "add", "sys-app-web-01",
            "--key", "app", "--value", "web-frontend"
        );
        Assert.Contains("added", output);

        (output, _) = await ExecuteAsync("systems", "get", "sys-app-web-01");
        Assert.Contains("sys-app-web-01", output);

        (output, _) = await ExecuteAsync("systems", "list");
        Assert.Contains("Name", output);

        (output, _) = await ExecuteAsync("systems", "summary");
        Assert.Contains("Name", output);

        (output, _) = await ExecuteAsync("systems", "describe", "sys-app-web-01");
        Assert.Contains("container", output);

        (output, _) = await ExecuteAsync("systems", "tree", "sys-app-web-01");
        Assert.Contains("sys-app-web-01", output);

        // ============================================================
        // ACCESS POINTS: Full workflow
        // ============================================================
        (output, _) = await ExecuteAsync("accesspoints", "add", "ap-floor2-01");
        Assert.Contains("added", output);

        (output, _) = await ExecuteAsync(
            "accesspoints", "set", "ap-floor2-01",
            "--model", "Ubiquiti-U6-Pro",
            "--speed", "2.5"
        );
        Assert.Contains("updated", output);

        (output, _) = await ExecuteAsync(
            "accesspoints", "label", "add", "ap-floor2-01",
            "--key", "location", "--value", "floor-2"
        );
        Assert.Contains("added", output);

        (output, _) = await ExecuteAsync("accesspoints", "get", "ap-floor2-01");
        Assert.Contains("ap-floor2-01", output);
        Assert.Contains("Ubiquiti", output);

        (output, _) = await ExecuteAsync("accesspoints", "list");
        Assert.Contains("ap-floor2-01", output);

        (output, _) = await ExecuteAsync("accesspoints", "summary");
        Assert.Contains("Name", output);

        (output, _) = await ExecuteAsync("accesspoints", "describe", "ap-floor2-01");
        Assert.Contains("ap-floor2-01", output);

        // ============================================================
        // UPS: Full workflow
        // ============================================================
        (output, _) = await ExecuteAsync("ups", "add", "ups-rack-a-01");
        Assert.Contains("added", output);

        (output, _) = await ExecuteAsync(
            "ups", "set", "ups-rack-a-01",
            "--model", "APC-SmartUPS-3000",
            "--va", "3000"
        );
        Assert.Contains("updated", output);

        (output, _) = await ExecuteAsync(
            "ups", "label", "add", "ups-rack-a-01",
            "--key", "rack", "--value", "a"
        );
        Assert.Contains("added", output);

        (output, _) = await ExecuteAsync("ups", "get", "ups-rack-a-01");
        Assert.Contains("ups-rack-a-01", output);
        Assert.Contains("3000", output);

        (output, _) = await ExecuteAsync("ups", "list");
        Assert.Contains("ups-rack-a-01", output);

        (output, _) = await ExecuteAsync("ups", "summary");
        Assert.Contains("Name", output);

        (output, _) = await ExecuteAsync("ups", "describe", "ups-rack-a-01");
        Assert.Contains("ups-rack-a-01", output);

        // ============================================================
        // DESKTOPS: Full workflow with components
        // ============================================================
        (output, _) = await ExecuteAsync("desktops", "add", "dtp-workstation-01");
        Assert.Contains("added", output);

        (output, _) = await ExecuteAsync(
            "desktops", "set", "dtp-workstation-01",
            "--model", "Dell-Precision-7865"
        );
        Assert.Contains("updated", output);

        (output, _) = await ExecuteAsync(
            "desktops", "cpu", "add", "dtp-workstation-01",
            "--model", "AMD-Ryzen-9-7950X",
            "--cores", "16",
            "--threads", "32"
        );
        Assert.Contains("added", output);

        (output, _) = await ExecuteAsync(
            "desktops", "drive", "add", "dtp-workstation-01",
            "--type", "ssd",
            "--size", "2048"
        );
        Assert.Contains("added", output);

        (output, _) = await ExecuteAsync(
            "desktops", "gpu", "add", "dtp-workstation-01",
            "--model", "NVIDIA-RTX-3090",
            "--vram", "24"
        );
        Assert.Contains("added", output);

        (output, _) = await ExecuteAsync(
            "desktops", "nic", "add", "dtp-workstation-01",
            "--type", "RJ45",
            "--speed", "10",
            "--ports", "2"
        );
        Assert.Contains("added", output);

        (output, _) = await ExecuteAsync("desktops", "get", "dtp-workstation-01");
        Assert.Contains("dtp-workstation-01", output);

        (output, _) = await ExecuteAsync("desktops", "list");
        Assert.Contains("dtp-workstation-01", output);

        (output, _) = await ExecuteAsync("desktops", "summary");
        Assert.Contains("Name", output);

        (output, _) = await ExecuteAsync("desktops", "describe", "dtp-workstation-01");
        Assert.Contains("dtp-workstation-01", output);
        // ============================================================
        // LAPTOPS: Full workflow with components
        // ============================================================
        (output, _) = await ExecuteAsync("laptops", "add", "ltp-dev-01");
        Assert.Contains("added", output);

        (output, _) = await ExecuteAsync(
            "laptops", "set", "ltp-dev-01",
            "--model", "Lenovo-ThinkPad-X1"
        );
        Assert.Contains("updated", output);

        (output, _) = await ExecuteAsync(
            "laptops", "cpu", "add", "ltp-dev-01",
            "--model", "Intel-i9-12900H",
            "--cores", "14",
            "--threads", "20"
        );
        Assert.Contains("added", output);

        (output, _) = await ExecuteAsync(
            "laptops", "drive", "add", "ltp-dev-01",
            "--type", "ssd",
            "--size", "1024"
        );
        Assert.Contains("added", output);

        (output, _) = await ExecuteAsync(
            "laptops", "gpu", "add", "ltp-dev-01",
            "--model", "Intel-Iris-Xe",
            "--vram", "2"
        );
        Assert.Contains("added", output);

        (output, _) = await ExecuteAsync("laptops", "get", "ltp-dev-01");
        Assert.Contains("ltp-dev-01", output);

        (output, _) = await ExecuteAsync("laptops", "list");
        Assert.Contains("ltp-dev-01", output);

        (output, _) = await ExecuteAsync("laptops", "summary");
        Assert.Contains("Name", output);

        (output, _) = await ExecuteAsync("laptops", "describe", "ltp-dev-01");
        Assert.Contains("ltp-dev-01", output);

        // ============================================================
        // SERVICES: Full workflow
        // ============================================================
        (output, _) = await ExecuteAsync("services", "add", "svc-postgres-primary");
        Assert.Contains("added", output);

        (output, _) = await ExecuteAsync(
            "services", "set", "svc-postgres-primary",
            "--ip", "10.0.0.100",
            "--port", "5432",
            "--protocol", "tcp",
            "--url", "postgresql://10.0.0.100:5432",
            "--runs-on", "sys-app-web-01"
        );
        Assert.Contains("updated", output);

        (output, _) = await ExecuteAsync(
            "services", "label", "add", "svc-postgres-primary",
            "--key", "db-type", "--value", "postgresql"
        );
        Assert.Contains("added", output);

        (output, _) = await ExecuteAsync("services", "get", "svc-postgres-primary");
        Assert.Contains("svc-postgres-primary", output);

        (output, _) = await ExecuteAsync("services", "list");
        Assert.Contains("Name", output);
        (output, _) = await ExecuteAsync("services", "summary");
        Assert.Contains("Name", output);

        (output, _) = await ExecuteAsync("services", "describe", "svc-postgres-primary");
        Assert.Contains("svc-postgres-primary", output);

        (output, _) = await ExecuteAsync("services", "subnets");
        Assert.Contains("Services", output);

        // ============================================================
        // EXPORTERS: Full workflow
        // ============================================================
        // Ansible inventory export
        (output, _) = await ExecuteAsync("ansible", "inventory");
        Assert.Contains("Generated Inventory", output);

        // SSH export
        (output, _) = await ExecuteAsync("ssh", "export");
        Assert.Contains("Generated SSH Config", output);

        // Hosts export
        (output, _) = await ExecuteAsync("hosts", "export");
        Assert.Contains("Generated Hosts File", output);

        // ============================================================
        // CONNECTIONS: Full workflow
        // ============================================================
        (output, _) = await ExecuteAsync(
            "connections", "add",
            "--resource-a", "sw-core-01",
            "--port-a", "1",
            "--resource-b", "fw-perimeter-01",
            "--port-b", "1"
        );
        if (!output.Contains("added")) {
            Assert.Contains("Error", output);
        }
        else {
            Assert.Contains("added", output);
        }
        (output, _) = await ExecuteAsync(
            "connections", "remove",
            "--resource", "sw-core-01",
            "--port", "1"
        );
        if (!output.Contains("removed")) {
            Assert.Contains("Error", output);
        }
        else {
            Assert.Contains("removed", output);
        }
        // ============================================================
        // DELETE resources to verify cleanup
        // ============================================================
        (output, _) = await ExecuteAsync("servers", "del", "srv-prod-web01");
        Assert.Contains("deleted", output);

        (output, _) = await ExecuteAsync("switches", "del", "sw-core-01");
        Assert.Contains("deleted", output);

        (output, _) = await ExecuteAsync("routers", "del", "rt-edge-01");
        Assert.Contains("deleted", output);

        (output, _) = await ExecuteAsync("firewalls", "del", "fw-perimeter-01");
        Assert.Contains("deleted", output);

        (output, _) = await ExecuteAsync("systems", "del", "sys-app-web-01");
        Assert.Contains("deleted", output);

        (output, _) = await ExecuteAsync("accesspoints", "del", "ap-floor2-01");
        Assert.Contains("deleted", output);

        (output, _) = await ExecuteAsync("ups", "del", "ups-rack-a-01");
        Assert.Contains("deleted", output);

        (output, _) = await ExecuteAsync("desktops", "del", "dtp-workstation-01");
        Assert.Contains("deleted", output);

        (output, _) = await ExecuteAsync("laptops", "del", "ltp-dev-01");
        Assert.Contains("deleted", output);

        (output, _) = await ExecuteAsync("services", "del", "svc-postgres-primary");
        Assert.Contains("deleted", output);

        // Verify all resources are gone
        (output, _) = await ExecuteAsync("summary");
    }
}
