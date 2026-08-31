using TacxRpmApp.Core.ViewModels;

namespace TacxRpmApp.Tests.ViewModels;

public class ConfigViewModelTests
{
    [Theory]
    [InlineData(0, 0, 0, 1, true)]
    [InlineData(200, 200, 200, 1, true)]
    [InlineData(-1, 0, 0, 1, false)]
    [InlineData(201, 0, 0, 1, false)]
    [InlineData(0, 0, 0, 0, false)]
    public void IsValid_EnforcesFloorAndCeiling(int uphill, int cruise, int downhill, int increment, bool expected)
        => Assert.Equal(expected, ConfigViewModel.IsValid(uphill, cruise, downhill, increment));
}
