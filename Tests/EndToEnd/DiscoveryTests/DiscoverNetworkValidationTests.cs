using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.DiscoveryTests;

/// <summary>
///     `rpk discover network` argument validation. Every case here fails before any
///     probing starts, so these tests never send a packet anywhere.
/// </summary>
[Collection("Process-wide static state")]
public class DiscoverNetworkValidationTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture> {
    private async Task<string> ExecuteAsync(params string[] args) =>
        await YamlCliTestHost.RunAsync(args, fs.Root, outputHelper, "config.yaml");

    [Theory]
    [InlineData("not-a-cidr")]
    [InlineData("192.168.1.0")] // no prefix
    [InlineData("192.168.1.0/24/7")]
    [InlineData("192.168.1.0/notanumber")]
    public async Task a_malformed_cidr_is_refused_with_an_example_of_the_right_shape(string cidr) {
        var output = await ExecuteAsync("discover", "network", "--cidr", cidr);

        Assert.Contains("not a usable CIDR block", output);
        Assert.Contains("192.168.1.0/24", output);
    }

    [Theory]
    [InlineData("10.0.0.0/8")]
    [InlineData("0.0.0.0/0")]
    public async Task a_sweep_wider_than_a_16_is_refused(string cidr) {
        var output = await ExecuteAsync("discover", "network", "--cidr", cidr);

        Assert.Contains("65,534 hosts", output);
        Assert.Contains("/16", output);
    }

    [Theory]
    [InlineData("eighty")]
    [InlineData("0")] // port zero is not a port
    [InlineData("65536")]
    [InlineData("22;80")]
    [InlineData(",")]
    public async Task a_malformed_port_list_is_refused(string ports) {
        var output = await ExecuteAsync(
            "discover", "network", "--cidr", "192.168.1.0/24", "--ports", ports);

        Assert.Contains("not a usable port list", output);
    }

    [Theory]
    [InlineData("--timeout", "0", "--timeout must be between")]
    [InlineData("--timeout", "999999", "--timeout must be between")]
    [InlineData("--parallel", "0", "--parallel must be between")]
    [InlineData("--parallel", "4096", "--parallel must be between")]
    public async Task out_of_range_tuning_flags_are_refused(string flag, string value, string expected) {
        var output = await ExecuteAsync("discover", "network", "--cidr", "192.168.1.0/24", flag, value);

        Assert.Contains(expected, output);
    }
}
