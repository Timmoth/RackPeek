using RackPeek.Domain.Discovery;

namespace Tests.Discovery;

/// <summary>
///     Identity has to be deterministic (the same machine gets the same id forever,
///     with nothing stored locally) and opaque (a config file gets committed to public
///     repositories, so raw machine ids and MAC addresses must not appear in it).
/// </summary>
public class DiscoveryIdentityTests {
    [Fact]
    public void The_same_seed_always_produces_the_same_id() =>
        Assert.Equal(
            DiscoveryId.Create(DiscoveryId.SystemScheme, "machine-a"),
            DiscoveryId.Create(DiscoveryId.SystemScheme, "machine-a"));

    [Fact]
    public void Different_seeds_produce_different_ids() =>
        Assert.NotEqual(
            DiscoveryId.Create(DiscoveryId.SystemScheme, "machine-a"),
            DiscoveryId.Create(DiscoveryId.SystemScheme, "machine-b"));

    [Fact]
    public void The_same_seed_in_different_schemes_stays_distinct() =>
        Assert.NotEqual(
            DiscoveryId.Create(DiscoveryId.SystemScheme, "seed"),
            DiscoveryId.Create(DiscoveryId.DockerScheme, "seed"));

    [Fact]
    public void The_seed_is_not_recoverable_by_reading_the_id() {
        var machineId = "7f3c9a1e5b2d4f6081a3c5e7b9d1f3a5";

        Assert.DoesNotContain(machineId, DiscoveryId.Create(DiscoveryId.SystemScheme, machineId));
    }

    [Fact]
    public void Ids_match_the_shape_the_published_schema_requires() =>
        Assert.Matches("^rpk[0-9]+:[a-z0-9]+:[0-9a-f]{16}$", DiscoveryId.Create("sys", "seed"));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_empty_seed_is_refused_rather_than_producing_a_shared_id(string seed) =>
        Assert.Throws<ArgumentException>(() => DiscoveryId.Create("sys", seed));

    [Theory]
    [InlineData("NAS01", "nas01")]
    [InlineData("Tims-MacBook-Pro", "tims-macbook-pro")]
    [InlineData("paperless ngx", "paperless-ngx")]
    [InlineData("Paperless_Stack", "paperless-stack")]
    [InlineData("  spaced  out  ", "spaced-out")]
    [InlineData("--dashes--", "dashes")]
    [InlineData("!!!", "")]
    public void Names_are_slugged_the_way_a_person_would_type_them(string input, string expected) =>
        Assert.Equal(expected, DiscoveryNaming.Slug(input));

    [Theory]
    [InlineData("nas01.lan", "nas01")]
    [InlineData("tims-macbook-pro.local", "tims-macbook-pro")]
    [InlineData("nas01", "nas01")]
    [InlineData("", "")]
    public void A_host_name_is_reduced_to_its_first_label(string input, string expected) =>
        Assert.Equal(expected, DiscoveryNaming.HostLabel(input));

    [Fact]
    public void A_machine_with_no_usable_name_still_gets_a_deterministic_one() {
        var id = DiscoveryId.Create("sys", "seed");

        var first = DiscoveryNaming.Suggest("???", "system", id);
        var second = DiscoveryNaming.Suggest(null, "system", id);

        Assert.Equal(first, second);
        Assert.StartsWith("system-", first);
    }

    // RackPeek's own validation caps a resource name at 50 characters, and the import
    // API does not enforce it — so a longer name is accepted and then cannot be renamed
    // or edited from the CLI. Discovery has to stay inside the limit on its own.

    [Fact]
    public void A_long_container_name_is_cut_to_a_length_rackpeek_accepts() {
        var id = DiscoveryId.Create(DiscoveryId.DockerScheme, "seed");

        var name = DiscoveryNaming.Suggest(
            "homeassistant-production-stack-mosquitto-broker-primary-1", "service", id);

        Assert.True(name.Length <= DiscoveryNaming.MaxNameLength, name);
        Assert.DoesNotContain("--", name);
        Assert.False(name.EndsWith('-'));
    }

    [Fact]
    public void A_disambiguating_suffix_shortens_the_name_to_make_room() {
        var suffixed = DiscoveryNaming.WithSuffix(new string('a', 48), "a3f9c2e1");

        Assert.True(suffixed.Length <= DiscoveryNaming.MaxNameLength, suffixed);
        Assert.EndsWith("-a3f9c2e1", suffixed);
    }

    [Fact]
    public void Two_names_that_truncate_alike_stay_distinct() {
        // Compose puts the replica index last, which is exactly what truncation removes.
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var prefix = "homeassistant-production-stack-mosquitto-broker-primary";

        var first = DiscoveryNaming.Unique(
            DiscoveryNaming.Suggest($"{prefix}-1", "service", DiscoveryId.Create("docker", "a")),
            DiscoveryId.Create("docker", "a"),
            taken);

        var second = DiscoveryNaming.Unique(
            DiscoveryNaming.Suggest($"{prefix}-2", "service", DiscoveryId.Create("docker", "b")),
            DiscoveryId.Create("docker", "b"),
            taken);

        Assert.NotEqual(first, second);
        Assert.True(second.Length <= DiscoveryNaming.MaxNameLength, second);
    }

    [Theory]
    [InlineData("nas01")]
    [InlineData("a-really-long-hostname-that-somebody-actually-configured-somewhere")]
    [InlineData("!!!")]
    [InlineData("")]
    public void Every_suggested_name_is_valid_to_rackpeek(string input) {
        var id = DiscoveryId.Create(DiscoveryId.SystemScheme, "seed");

        var name = DiscoveryNaming.Suggest(input, "system", id);

        // The same checks ThrowIfInvalid.ResourceName makes.
        Assert.False(string.IsNullOrWhiteSpace(name));
        Assert.True(name.Length <= DiscoveryNaming.MaxNameLength, name);
    }

    [Theory]
    [InlineData("日本語サーバー")]
    [InlineData("сервер")]
    [InlineData("🎉🎉🎉")]
    public void A_name_with_nothing_ascii_in_it_falls_back_to_the_id(string input) {
        var id = DiscoveryId.Create(DiscoveryId.SystemScheme, "seed");

        var name = DiscoveryNaming.Suggest(input, "system", id);

        // Slugging keeps to ASCII, so these produce nothing usable and the id-derived
        // name takes over — still deterministic, still valid, still unique.
        Assert.Equal($"system-{DiscoveryId.ShortSuffix(id)}", name);
    }
}
