using TacxRpmApp.Core.Protocol;

namespace TacxRpmApp.Tests.Protocol;

public class FeCPageParserTests
{
    [Fact]
    public void Parse_CommandStatusPage_DecodesStatusByteAtIndexSeven()
    {
        // Captured fixture: status byte (index 7) is 0xFF -> Uninitialized.
        var packet = Convert.FromHexString("A4094E0547FFFFFF010000207F");
        var result = FeCPageParser.Parse(packet);

        Assert.Equal(FeCPage.CommandStatus, result.Page);
        Assert.Equal(CommandStatus.Uninitialized, result.CommandStatus);
    }

    [Fact]
    public void Parse_CommandStatusPage_SuccessByte_ReturnsSuccess()
    {
        var packet = Convert.FromHexString("A4094E0547FFFF00010000207F");
        var result = FeCPageParser.Parse(packet);

        Assert.Equal(FeCPage.CommandStatus, result.Page);
        Assert.Equal(CommandStatus.Success, result.CommandStatus);
    }

    [Fact]
    public void Parse_CapabilitiesPage_ReturnsMaximumResistance()
    {
        var packet = Convert.FromHexString("A4094E0536FFFFFFFF0A00FF00");
        var result = FeCPageParser.Parse(packet);

        Assert.Equal(FeCPage.Capabilities, result.Page);
        Assert.Equal((ushort)10, result.MaximumResistance);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A4094E")]
    [InlineData("FF094E0547FFFFFF010000207F")]
    public void Parse_MalformedPacket_ReturnsUnknown(string hex)
        => Assert.Equal(FeCPage.Unknown, FeCPageParser.Parse(Convert.FromHexString(hex)).Page);
}
