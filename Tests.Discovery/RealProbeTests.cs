using RackPeek.Domain.Discovery;
using RackPeek.Domain.Resources.SystemResources;

namespace Tests.Discovery;

/// <summary>
///     Everywhere else in this project the probes are bypassed and the parser is driven
///     from fixtures. These tests do the opposite: they run the real probe against the
///     real machine, which is the only way to catch a wrong path or a changed command.
///     Each one is skipped off its own platform, so the CI matrix covers Linux on the
///     ubuntu runner and macOS on the macos runner.
/// </summary>
public class RealProbeTests {
    [Fact]
    public async Task The_linux_probe_reads_this_machine() {
        // No-op off Linux. xUnit v2 has no skip-at-runtime, and a custom attribute is
        // more machinery than this needs — the CI matrix is what makes it run.
        if (!OperatingSystem.IsLinux())
            return;

        RawSystemSnapshot snapshot = await new LinuxSystemProbe().ReadAsync(CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(snapshot.Hostname));
        Assert.True(snapshot.Cores > 0);

        // Each of these guards a hard-coded path. If one is wrong the probe silently
        // returns null and the resource quietly loses a field, which no fixture can catch.
        AssertReadIfPresent("/etc/os-release", snapshot.OsReleaseFile);
        AssertReadIfPresent("/proc/meminfo", snapshot.MemInfoFile);
        AssertReadIfPresent("/proc/1/cgroup", snapshot.CgroupFile);
        AssertReadIfPresent("/etc/machine-id", snapshot.MachineIdFile);
        AssertReadIfPresent("/sys/class/dmi/id/sys_vendor", snapshot.DmiVendor);
        AssertReadIfPresent("/sys/class/dmi/id/product_name", snapshot.DmiProduct);

        if (Directory.Exists("/sys/block") && Directory.EnumerateDirectories("/sys/block").Any())
            Assert.NotEmpty(snapshot.BlockDevices);

        AssertUsable(SystemFactsParser.Parse(snapshot));
    }

    [Fact]
    public async Task The_macos_probe_reads_this_machine() {
        if (!OperatingSystem.IsMacOS())
            return;

        RawSystemSnapshot snapshot = await new MacSystemProbe().ReadAsync(CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(snapshot.Hostname));
        Assert.True(snapshot.Cores > 0);

        // These come from sw_vers, sysctl and ioreg — all shell-outs, none of which a
        // fixture can prove are still spelled correctly.
        Assert.StartsWith("macOS", snapshot.OsName);
        Assert.True(snapshot.MemoryBytes > 0);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.PlatformUuid));

        AssertUsable(SystemFactsParser.Parse(snapshot));
    }

    [Fact]
    public void Exactly_one_probe_claims_this_platform() {
        ISystemProbe[] probes = [new LinuxSystemProbe(), new MacSystemProbe()];

        var supported = probes.Count(p => p.IsSupported);

        Assert.Equal(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() ? 1 : 0, supported);
    }

    /// <summary>
    ///     Reads the file itself and, if this machine actually has content there, insists
    ///     the probe found it too. Note the test has to read rather than stat: everything
    ///     under /proc reports a length of zero, so a size check silently passes and
    ///     covers none of the paths that matter most.
    /// </summary>
    private static void AssertReadIfPresent(string path, string? value) {
        string? actual;

        try {
            actual = File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch {
            return; // Present but unreadable for this user; nothing to hold the probe to.
        }

        if (string.IsNullOrWhiteSpace(actual))
            return;

        Assert.False(
            string.IsNullOrWhiteSpace(value),
            $"{path} has content on this machine but the probe read nothing from it.");
    }

    private static void AssertUsable(SystemFacts facts) {
        Assert.Contains(facts.Type, SystemResource.ValidSystemTypes);
        Assert.NotEqual("Unknown", facts.Os);
        Assert.True(facts.RamGb > 0);

        // The schema requires type, os, cores and ram, so a real machine has to produce
        // something importable rather than a half-filled resource.
        Fixture.AssertConformsToSchema(
            DiscoveryDocument.ToYaml([SystemResourceMapper.ToResource(facts)]));
    }
}
