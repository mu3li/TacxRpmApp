using TacxRpmApp.Core.Protocol;

namespace TacxRpmApp.Tests.Protocol;

public class HeartRateMeasurementParserTests
{
    [Theory]
    [InlineData("0048", 72)]           // 8-bit, flags bit 0 clear
    [InlineData("01B400", 180)]        // 16-bit little-endian, flags bit 0 set
    public void Parse_ValidMeasurement_ReturnsBpm(string hex, int expected)
        => Assert.Equal(expected, HeartRateMeasurementParser.Parse(Convert.FromHexString(hex)));

    [Theory]
    [InlineData("0000")]               // zero
    [InlineData("012D01")]             // 301
    public void Parse_ImplausibleValue_ReturnsNull(string hex)
        => Assert.Null(HeartRateMeasurementParser.Parse(Convert.FromHexString(hex)));
}
