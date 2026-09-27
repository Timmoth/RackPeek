using RackPeek.Domain.Resources.Services.Networking;

namespace Tests.Discovery;

/// <summary>
///     CIDR parsing feeds the sweep its targets, so leniency here means probing a
///     network the user never named: unchecked octet arithmetic used to fold
///     192.168.256.0 into 192.169.0.0 and call it usable.
/// </summary>
public class CidrParsingTests {
    [Theory]
    [InlineData("192.168.1.0/24", "192.168.1.0/24")]
    [InlineData("192.168.1.37/24", "192.168.1.0/24")] // a host address masks down
    [InlineData("10.0.0.0/8", "10.0.0.0/8")]
    [InlineData("127.0.0.1/32", "127.0.0.1/32")]
    public void Valid_blocks_parse_and_mask_to_their_network(string input, string expected) {
        Assert.True(Cidr.TryParse(input, out Cidr cidr));
        Assert.Equal(expected, cidr.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-cidr")]
    [InlineData("192.168.1.0")] // no prefix
    [InlineData("192.168.1.0/24/7")]
    [InlineData("192.168.1.0/notanumber")]
    [InlineData("192.168.1.0/33")]
    [InlineData("192.168.256.0/24")] // octet overflow must not wrap into .169
    [InlineData("192.-1.1.0/24")]
    [InlineData("300.1.1.1/24")]
    [InlineData("1.2.3/24")]
    public void Anything_else_is_refused_rather_than_reinterpreted(string? input) =>
        Assert.False(Cidr.TryParse(input, out _));
}
