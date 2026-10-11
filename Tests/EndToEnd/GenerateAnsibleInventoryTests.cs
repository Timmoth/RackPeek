using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd;

[Collection("Process-wide static state")]
public class GenerateAnsibleInventoryTests(
    TempYamlCliFixture fs,
    ITestOutputHelper outputHelper)
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

    [Fact]
    public async Task generate_ansible_inventory_empty_config() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
version: 1
resources: []
""");

        (var output, _) = await ExecuteAsync("ansible", "inventory");

        Assert.Contains("Generated Inventory", output);
    }

    [Fact]
    public async Task generate_ansible_inventory_single_system_ini_format() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
version: 1
resources:
  - kind: System
    type: vm
    name: web-server-01
    os: ubuntu-22.04
    cores: 2
    ram: 4
    labels:
      ansible_host: 192.168.1.100
      ansible_user: admin
      env: production
""");

        (var output, _) = await ExecuteAsync("ansible", "inventory", "--group-labels", "env");

        Assert.Contains("Generated Inventory", output);
        Assert.Contains("web-server-01", output);
        Assert.Contains("ansible_host=192.168.1.100", output);
        Assert.Contains("ansible_user=admin", output);
        Assert.Contains("[env_production]", output);
    }

    [Fact]
    public async Task generate_ansible_inventory_with_tag_grouping() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
version: 1
resources:
  - kind: System
    type: vm
    name: web-prod-01
    os: ubuntu-22.04
    cores: 2
    ram: 4
    tags: [production, web]
    labels:
      ansible_host: 10.0.1.10
      ansible_user: ubuntu

  - kind: System
    type: vm
    name: web-prod-02
    os: ubuntu-22.04
    cores: 2
    ram: 4
    tags: [production, web]
    labels:
      ansible_host: 10.0.1.11
      ansible_user: ubuntu

  - kind: System
    type: vm
    name: db-staging-01
    os: postgres-15
    cores: 4
    ram: 8
    tags: [staging, database]
    labels:
      ansible_host: 10.0.2.20
      ansible_user: postgres
""");

        (var output, _) = await ExecuteAsync(
            "ansible", "inventory",
            "--group-tags", "production,staging,web,database");

        Assert.Contains("[production]", output);
        Assert.Contains("[staging]", output);
        Assert.Contains("[web]", output);
        Assert.Contains("[database]", output);
        Assert.Contains("web-prod-01", output);
        Assert.Contains("web-prod-02", output);
        Assert.Contains("db-staging-01", output);
    }

    [Fact]
    public async Task generate_ansible_inventory_with_label_grouping() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
version: 1
resources:
  - kind: System
    type: vm
    name: server-east-01
    os: debian-12
    cores: 2
    ram: 4
    labels:
      ansible_host: 10.0.1.10
      region: us-east

  - kind: System
    type: vm
    name: server-west-01
    os: debian-12
    cores: 2
    ram: 4
    labels:
      ansible_host: 10.0.2.10
      region: us-west

  - kind: System
    type: vm
    name: server-eu-01
    os: debian-12
    cores: 2
    ram: 4
    labels:
      ansible_host: 10.0.3.10
      region: eu-central
""");

        (var output, _) = await ExecuteAsync(
            "ansible", "inventory",
            "--group-labels", "region");

        Assert.Contains("[region_us_east]", output);
        Assert.Contains("[region_us_west]", output);
        Assert.Contains("[region_eu_central]", output);
        Assert.Contains("server-east-01", output);
        Assert.Contains("server-west-01", output);
        Assert.Contains("server-eu-01", output);
    }

    [Fact]
    public async Task generate_ansible_inventory_with_global_vars() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
version: 1
resources:
  - kind: System
    type: vm
    name: app-server-01
    os: ubuntu-22.04
    cores: 2
    ram: 4
    labels:
      ansible_host: 192.168.1.50
      ansible_user: deploy
      env: prod
""");

        (var output, _) = await ExecuteAsync(
            "ansible", "inventory",
            "--group-labels", "env",
            "--global-var", "ansible_ssh_common_args='-o StrictHostKeyChecking=no'",
            "--global-var", "python_version=3.10",
            "--global-var", "app_name=myapp");

        Assert.Contains("Generated Inventory", output);
        Assert.Contains("ansible_ssh_common_args", output);
        Assert.Contains("StrictHostKeyChecking=no", output);
        Assert.Contains("python_version=3.10", output);
        Assert.Contains("app_name=myapp", output);
        Assert.Contains("app-server-01", output);
    }

    [Fact]
    public async Task generate_ansible_inventory_yaml_format() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
version: 1
resources:
  - kind: System
    type: vm
    name: ansible-test-host
    os: ubuntu-22.04
    cores: 2
    ram: 4
    labels:
      ansible_host: 172.16.0.100
      ansible_user: root
      env: test
""");

        (var output, _) = await ExecuteAsync("ansible", "inventory", "--format", "yaml", "--group-labels", "env");

        Assert.Contains("Generated Inventory", output);
        Assert.Contains("all:", output);
        Assert.Contains("children:", output);
        Assert.Contains("ansible-test-host:", output);
        Assert.Contains("ansible_host: 172.16.0.100", output);
        Assert.DoesNotContain("[all:vars]", output);
    }

    [Fact]
    public async Task generate_ansible_inventory_combined_grouping() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
version: 1
resources:
  - kind: System
    type: vm
    name: prod-web-01
    os: ubuntu-22.04
    cores: 2
    ram: 4
    tags: [production]
    labels:
      ansible_host: 10.0.1.10
      env: production
      tier: web

  - kind: System
    type: vm
    name: prod-db-01
    os: postgres-15
    cores: 4
    ram: 8
    tags: [production]
    labels:
      ansible_host: 10.0.1.20
      env: production
      tier: database
""");

        (var output, _) = await ExecuteAsync(
            "ansible", "inventory",
            "--group-tags", "production",
            "--group-labels", "env,tier");

        Assert.Contains("[production]", output);
        Assert.Contains("[env_production]", output);
        Assert.Contains("[tier_web]", output);
        Assert.Contains("[tier_database]", output);
        Assert.Contains("prod-web-01", output);
        Assert.Contains("prod-db-01", output);
    }

    [Fact]
    public async Task generate_ansible_inventory_mixed_resources() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
version: 1
resources:
  - kind: Server
    name: srv-prod-01
    labels:
      ansible_host: 192.168.1.10
      ansible_user: root
      env: production

  - kind: System
    type: vm
    name: vm-dev-01
    os: ubuntu-22.04
    cores: 2
    ram: 4
    labels:
      ansible_host: 192.168.1.20
      ansible_user: developer
      env: development

  - kind: Desktop
    name: dtp-admin-01
    labels:
      ansible_host: 192.168.1.30
      ansible_user: admin
      env: production

  - kind: Laptop
    name: ltp-remote-01
    labels:
      ansible_host: 192.168.1.40
      ansible_user: remote
      env: remote

  - kind: Switch
    name: sw-access-01
    labels:
      ansible_host: 192.168.1.50
      ansible_user: network
      env: network
""");

        (var output, _) = await ExecuteAsync("ansible", "inventory", "--group-labels", "env");

        Assert.Contains("Generated Inventory", output);
        Assert.Contains("srv-prod-01", output);
        Assert.Contains("vm-dev-01", output);
        Assert.Contains("dtp-admin-01", output);
        Assert.Contains("ltp-remote-01", output);
        Assert.Contains("sw-access-01", output);
    }

    [Fact]
    public async Task generate_ansible_inventory_with_multiple_labels() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
version: 1
resources:
  - kind: System
    type: vm
    name: multi-label-host
    os: ubuntu-22.04
    cores: 2
    ram: 4
    labels:
      ansible_host: 10.0.0.1
      ansible_user: sysadmin
      environment: prod
      team: backend
      os: ubuntu
""");

        (var output, _) = await ExecuteAsync(
            "ansible", "inventory",
            "--group-labels", "environment,team,os");

        Assert.Contains("[environment_prod]", output);
        Assert.Contains("[team_backend]", output);
        Assert.Contains("[os_ubuntu]", output);
        Assert.Contains("multi-label-host", output);
    }

    [Fact]
    public async Task generate_ansible_inventory_ansible_var_labels() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
version: 1
resources:
  - kind: System
    type: vm
    name: web-server-01
    os: ubuntu-22.04
    cores: 2
    ram: 4
    labels:
      ansible_host: 192.168.1.100
      ansible_user: deploy
      ansible_become: "yes"
      ansible_var_python_path: /usr/bin/python3
      ansible_var_site: production
      ansible_var_app_env: prod
      env: prod
""");

        (var output, _) = await ExecuteAsync("ansible", "inventory", "--group-labels", "env");

        Assert.Contains("web-server-01", output);
        Assert.Contains("ansible_host=192.168.1.100", output);
        Assert.Contains("ansible_user=deploy", output);
        Assert.Contains("ansible_become=yes", output);
        Assert.Contains("python_path=/usr/bin/python3", output);
        Assert.Contains("site=production", output);
        Assert.Contains("app_env=prod", output);
    }

    [Fact]
    public async Task generate_ansible_inventory_yaml_format_with_ansible_vars() {
        await File.WriteAllTextAsync(Path.Combine(fs.Root, "config.yaml"), """
version: 1
resources:
  - kind: System
    type: vm
    name: ansible-var-test
    os: ubuntu-22.04
    cores: 2
    ram: 4
    labels:
      ansible_host: 10.0.0.50
      ansible_var_custom_var: custom_value
      ansible_var_number: "42"
      env: test
""");

        (var output, _) = await ExecuteAsync(
            "ansible", "inventory",
            "--format", "yaml",
            "--group-labels", "env");

        Assert.Contains("ansible-var-test:", output);
        Assert.Contains("ansible_host: 10.0.0.50", output);
        Assert.Contains("custom_var: custom_value", output);
        Assert.Contains("number: 42", output);
    }
}
