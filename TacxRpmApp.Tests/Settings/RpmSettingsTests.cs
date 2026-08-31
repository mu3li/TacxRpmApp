using TacxRpmApp.Core.Settings;
using TacxRpmApp.Tests.Fakes;

namespace TacxRpmApp.Tests.Settings;

public class RpmSettingsTests
{
    [Fact]
    public void Defaults_MatchCurrentBuild()
    {
        var settings = new RpmSettings(new FakePreferenceStore());

        Assert.Equal(45, settings.Uphill);
        Assert.Equal(20, settings.Cruise);
        Assert.Equal(10, settings.Downhill);
        Assert.Equal(2, settings.Increment);
    }

    [Theory]
    [InlineData(-10, 0)]
    [InlineData(0, 0)]
    [InlineData(200, 200)]
    [InlineData(999, 200)]
    public void Presets_ClampToProtocolRange(int input, int expected)
    {
        var settings = new RpmSettings(new FakePreferenceStore());

        settings.Uphill = input;

        Assert.Equal(expected, settings.Uphill);
    }

    [Theory]
    [InlineData(-5, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(5, 5)]
    public void Increment_FloorsAtOne(int input, int expected)
    {
        var settings = new RpmSettings(new FakePreferenceStore());

        settings.Increment = input;

        Assert.Equal(expected, settings.Increment);
    }
}
