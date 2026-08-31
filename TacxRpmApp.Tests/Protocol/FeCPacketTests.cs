using TacxRpmApp.Core.Protocol;

namespace TacxRpmApp.Tests.Protocol;

public class FeCPacketTests
{
    [Fact]
    public void BasicResistance_ProducesThirteenByteVerifiedPacket()
    {
        var packet = FeCPacket.BasicResistance(45);

        Assert.Equal(13, packet.Length);
        Assert.Equal("A4094E0530FFFFFFFFFFFF2D", Convert.ToHexString(packet)[..24]);
        Assert.Equal(FeCChecksum.Of(packet.AsSpan(0, 12)), packet[12]);
    }

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(0, 0)]
    [InlineData(200, 200)]
    [InlineData(999, 200)]
    public void BasicResistance_ClampsToProtocolRange(int input, byte expected)
        => Assert.Equal(expected, FeCPacket.BasicResistance(input)[11]);
}
