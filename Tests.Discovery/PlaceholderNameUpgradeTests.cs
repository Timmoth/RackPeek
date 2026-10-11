using RackPeek.Domain.Discovery;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Connections;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;

namespace Tests.Discovery;

/// <summary>
///     A placeholder name is a stand-in, and a later collector that knows the machine's
///     real name should be allowed to replace it.
///     <para>
///         A sweep of a routed subnet can see that something answers and nothing else, so
///         it writes host-1a2b3c4d. Run the firewall collector and that same machine has
///         a DHCP name; ask the hypervisor and it has a guest name. Without this the
///         inventory would keep the hash for ever and the better name would be discarded
///         on every run.
///     </para>
///     <para>
///         The line it must not cross is a name a person typed. That is what
///         <see cref="Resource.UserNamed" /> records, and once set it is never unset.
///     </para>
/// </summary>
public class PlaceholderNameUpgradeTests {
    private const string _mac = "bc:24:11:00:3a:01";

    private static string PlaceholderFor(string discoveryId) =>
        $"host-{DiscoveryId.ShortSuffix(discoveryId)}";

    /// <summary>A sweep's card: an address, a MAC, and a name that is just its own hash.</summary>
    private static SystemResource Stored(string? name = null, bool? userNamed = null) {
        var id = DiscoveryId.Create(DiscoveryId.NetworkScheme, _mac);

        return new SystemResource {
            Kind = SystemResource.KindLabel,
            Name = name ?? PlaceholderFor(id),
            DiscoveryId = id,
            UserNamed = userNamed,
            Ip = "192.0.2.50",
            Labels = { ["mac"] = _mac }
        };
    }

    /// <summary>The firewall's view of the same box: same identity, but it knows the name.</summary>
    private static SystemResource Incoming(string name = "forgejo") => new() {
        Kind = SystemResource.KindLabel,
        Name = name,
        DiscoveryId = DiscoveryId.Create(DiscoveryId.NetworkScheme, _mac),
        Ip = "192.0.2.50",
        Labels = { ["mac"] = _mac }
    };

    private static List<Resource> Resolve(
        Resource stored,
        Resource incoming,
        IReadOnlyList<Connection>? storedConnections = null,
        params Resource[] alsoStored) {
        List<Resource> existing = [stored, .. alsoStored];

        DiscoveryIdResolver.ResolveNames(
            existing,
            [incoming],
            null,
            storedConnections,
            true);

        return existing;
    }

    [Fact]
    public void A_real_name_replaces_a_placeholder_nobody_chose() {
        SystemResource stored = Stored();

        Resolve(stored, Incoming());

        Assert.Equal("forgejo", stored.Name);
    }

    [Fact]
    public void A_name_a_person_typed_is_never_touched() {
        SystemResource stored = Stored("the-blue-one", true);

        Resolve(stored, Incoming());

        Assert.Equal("the-blue-one", stored.Name);
    }

    [Fact]
    public void A_placeholder_a_person_chose_to_keep_is_still_theirs() {
        // Renaming a card back to its hash is a strange thing to do, but it is a choice,
        // and the flag is what records that rather than the shape of the name.
        SystemResource stored = Stored(userNamed: true);
        var before = stored.Name;

        Resolve(stored, Incoming());

        Assert.Equal(before, stored.Name);
    }

    [Fact]
    public void A_placeholder_never_replaces_a_real_name() {
        // The reverse direction: the sweep runs after the firewall and knows less. The
        // upgrade is one-way or the card would flip names on alternate runs.
        SystemResource stored = Stored("forgejo");
        var placeholder = PlaceholderFor(stored.DiscoveryId!);

        Resolve(stored, Incoming(placeholder));

        Assert.Equal("forgejo", stored.Name);
    }

    [Fact]
    public void One_real_name_does_not_replace_another() {
        // Two collectors that each know a different name for one box would otherwise
        // rename it back and forth every run. First real name wins and stays.
        SystemResource stored = Stored("forgejo");

        Resolve(stored, Incoming("git-server"));

        Assert.Equal("forgejo", stored.Name);
    }

    [Fact]
    public void A_hand_written_resource_counts_as_user_named_without_the_flag() {
        // Nothing but a person could have written a resource with no discovery id, so
        // the absent flag must not be read as permission.
        var stored = new SystemResource {
            Kind = SystemResource.KindLabel,
            Name = "forgejo",
            Ip = "192.0.2.50"
        };

        Assert.True(stored.IsUserNamed());
        Assert.False(Stored().IsUserNamed());
    }

    [Fact]
    public void Everything_pointing_at_the_old_name_follows_it() {
        SystemResource stored = Stored();
        var placeholder = stored.Name;

        var service = new Service {
            Kind = Service.KindLabel,
            Name = "forgejo-https",
            RunsOn = [placeholder]
        };

        Resolve(stored, Incoming(), null, service);

        Assert.Equal(["forgejo"], service.RunsOn);
    }

    [Fact]
    public void Stored_connections_follow_it_too() {
        SystemResource stored = Stored();
        var placeholder = stored.Name;

        var connection = new Connection {
            A = new PortReference { Resource = placeholder },
            B = new PortReference { Resource = "core-switch", PortIndex = 12 }
        };

        Resolve(stored, Incoming(), [connection]);

        Assert.Equal("forgejo", connection.A.Resource);
    }

    [Fact]
    public void A_re_push_of_exported_yaml_follows_the_rename_too() {
        // The payload still calls the machine by the name it was stored under, because
        // that is what was exported. Its links have to land on the upgraded card.
        SystemResource stored = Stored();
        var placeholder = stored.Name;

        var arriving = new Service {
            Kind = Service.KindLabel,
            Name = "forgejo-https",
            DiscoveryId = DiscoveryId.Create(DiscoveryId.NetworkScheme, $"{_mac}:443"),
            RunsOn = [placeholder]
        };

        DiscoveryIdResolver.ResolveNames([stored], [Incoming(), arriving], null, null, true);

        Assert.Equal("forgejo", stored.Name);
        Assert.Equal(["forgejo"], arriving.RunsOn);
    }

    [Fact]
    public void A_service_named_after_the_host_follows_it() {
        // The sweep names what it finds on a port after the host it found it on, so the
        // leftover reads host-1a2b3c4d-ssh running on forgejo until this carries it over.
        SystemResource stored = Stored();
        var placeholder = stored.Name;

        var ssh = new Service {
            Kind = Service.KindLabel,
            Name = $"{placeholder}-ssh",
            DiscoveryId = DiscoveryId.Create(DiscoveryId.NetworkScheme, $"{_mac}:22"),
            RunsOn = [placeholder]
        };

        Resolve(stored, Incoming(), null, ssh);

        Assert.Equal("forgejo-ssh", ssh.Name);
        Assert.Equal(["forgejo"], ssh.RunsOn);
    }

    [Fact]
    public void A_service_a_person_named_keeps_its_name() {
        SystemResource stored = Stored();
        var placeholder = stored.Name;

        var ssh = new Service {
            Kind = Service.KindLabel,
            Name = $"{placeholder}-ssh",
            DiscoveryId = DiscoveryId.Create(DiscoveryId.NetworkScheme, $"{_mac}:22"),
            UserNamed = true,
            RunsOn = [placeholder]
        };

        Resolve(stored, Incoming(), null, ssh);

        Assert.Equal($"{placeholder}-ssh", ssh.Name);
    }

    [Fact]
    public void A_service_that_only_looks_like_the_host_is_not_renamed() {
        // Named for the old host but running somewhere else entirely: a coincidence,
        // and coincidences are not evidence of anything.
        SystemResource stored = Stored();
        var placeholder = stored.Name;

        var stray = new Service {
            Kind = Service.KindLabel,
            Name = $"{placeholder}-ssh",
            DiscoveryId = DiscoveryId.Create(DiscoveryId.NetworkScheme, "elsewhere:22"),
            RunsOn = ["some-other-box"]
        };

        Resolve(stored, Incoming(), null, stray);

        Assert.Equal($"{placeholder}-ssh", stray.Name);
    }

    [Fact]
    public void A_service_whose_new_name_is_taken_keeps_the_old_one() {
        SystemResource stored = Stored();
        var placeholder = stored.Name;

        var ssh = new Service {
            Kind = Service.KindLabel,
            Name = $"{placeholder}-ssh",
            DiscoveryId = DiscoveryId.Create(DiscoveryId.NetworkScheme, $"{_mac}:22"),
            RunsOn = [placeholder]
        };

        var occupier = new Service {
            Kind = Service.KindLabel,
            Name = "forgejo-ssh",
            DiscoveryId = DiscoveryId.Create(DiscoveryId.NetworkScheme, "other:22"),
            RunsOn = ["some-other-box"]
        };

        Resolve(stored, Incoming(), null, ssh, occupier);

        Assert.Equal($"{placeholder}-ssh", ssh.Name);
    }

    [Fact]
    public void A_connection_to_a_renamed_service_follows_it() {
        SystemResource stored = Stored();
        var placeholder = stored.Name;

        var ssh = new Service {
            Kind = Service.KindLabel,
            Name = $"{placeholder}-ssh",
            DiscoveryId = DiscoveryId.Create(DiscoveryId.NetworkScheme, $"{_mac}:22"),
            RunsOn = [placeholder]
        };

        var connection = new Connection {
            A = new PortReference { Resource = $"{placeholder}-ssh" },
            B = new PortReference { Resource = "core-switch", PortIndex = 3 }
        };

        DiscoveryIdResolver.ResolveNames(
            [stored, ssh], [Incoming()], null, [connection], true);

        Assert.Equal("forgejo-ssh", connection.A.Resource);
    }

    [Fact]
    public void A_name_already_in_use_is_left_alone() {
        // Renaming onto an occupied name would collide two unrelated resources in the
        // merge, which keys on name. Keeping the placeholder is the safe outcome.
        SystemResource stored = Stored();
        var placeholder = stored.Name;

        var other = new SystemResource {
            Kind = SystemResource.KindLabel,
            Name = "forgejo",
            Ip = "192.0.2.99"
        };

        Resolve(stored, Incoming(), null, other);

        Assert.Equal(placeholder, stored.Name);
    }

    [Fact]
    public void A_dry_run_leaves_the_stored_name_where_it_was() {
        // The upgrade rewrites the inventory, so a caller that is only reporting what
        // would happen must not ask for it — the default is off for exactly this reason.
        SystemResource stored = Stored();
        var placeholder = stored.Name;

        DiscoveryIdResolver.ResolveNames([stored], [Incoming()]);

        Assert.Equal(placeholder, stored.Name);
    }
}
