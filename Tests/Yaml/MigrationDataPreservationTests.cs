using RackPeek.Domain.Persistence.Yaml;

namespace Tests.Yaml;

public class MigrationDataPreservationTests {
    [Fact]
    public async Task RunsOn_migration_rejects_non_string_items_without_changing_them() {
        var runsOn = new List<object> { "hypervisor", 42 };
        var resource = new Dictionary<object, object> {
            ["kind"] = "System",
            ["name"] = "apps",
            ["runsOn"] = runsOn
        };
        var root = new Dictionary<object, object> {
            ["resources"] = new List<object> { resource }
        };

        await Assert.ThrowsAsync<InvalidCastException>(async () =>
            await RackPeekConfigMigrationDeserializer.ConvertScalarRunsOnToList(null!, root));

        Assert.Same(runsOn, resource["runsOn"]);
        Assert.Equal(new object[] { "hypervisor", 42 }, runsOn);
    }

    [Fact]
    public async Task Nic_migration_rejects_non_object_items_without_removing_the_original_data() {
        var nics = new List<object> {
            new Dictionary<object, object> {
                ["type"] = "rj45",
                ["speed"] = 1,
                ["ports"] = 2
            },
            "unexpected"
        };
        var resource = new Dictionary<object, object> {
            ["kind"] = "Server",
            ["name"] = "node",
            ["nics"] = nics
        };
        var root = new Dictionary<object, object> {
            ["resources"] = new List<object> { resource }
        };

        await Assert.ThrowsAsync<InvalidCastException>(async () =>
            await RackPeekConfigMigrationDeserializer.ConvertNicsToPortsV3(null!, root));

        Assert.Same(nics, resource["nics"]);
        Assert.False(resource.ContainsKey("ports"));
    }

    [Fact]
    public async Task Nic_migration_rejects_unknown_fields_instead_of_discarding_them() {
        var nic = new Dictionary<object, object> {
            ["type"] = "rj45",
            ["speed"] = 1,
            ["ports"] = 2,
            ["vendorField"] = "keep-me"
        };
        var resource = new Dictionary<object, object> {
            ["kind"] = "Server",
            ["name"] = "node",
            ["nics"] = new List<object> { nic }
        };
        var root = new Dictionary<object, object> {
            ["resources"] = new List<object> { resource }
        };

        await Assert.ThrowsAsync<InvalidCastException>(async () =>
            await RackPeekConfigMigrationDeserializer.ConvertNicsToPortsV3(null!, root));

        Assert.True(resource.ContainsKey("nics"));
        Assert.False(resource.ContainsKey("ports"));
        Assert.Equal("keep-me", nic["vendorField"]);
    }
}
