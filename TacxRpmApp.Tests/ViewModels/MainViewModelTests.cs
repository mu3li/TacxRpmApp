using TacxRpmApp.Core.Services;
using TacxRpmApp.Core.Settings;
using TacxRpmApp.Core.ViewModels;
using TacxRpmApp.Tests.Fakes;

namespace TacxRpmApp.Tests.ViewModels;

public class MainViewModelTests
{
    private static (MainViewModel ViewModel, FakeTrainerTransport Transport) CreateViewModel()
    {
        var transport = new FakeTrainerTransport();
        var trainer = new TrainerService(transport);
        var settings = new RpmSettings(new FakePreferenceStore());
        return (new MainViewModel(trainer, settings), transport);
    }

    [Fact]
    public async Task AdjustingNonActivePreset_DoesNotWriteToTransport()
    {
        var (viewModel, transport) = CreateViewModel();

        // No preset has been applied yet, so Cruise is not active.
        await viewModel.CruisePlusCommand.ExecuteAsync(null);

        Assert.Empty(transport.WrittenPackets);
    }

    [Fact]
    public async Task AdjustingActivePreset_WritesNewValueToTransport()
    {
        var (viewModel, transport) = CreateViewModel();

        await viewModel.CruiseApplyCommand.ExecuteAsync(null);
        transport.WrittenPackets.Clear();

        await viewModel.CruisePlusCommand.ExecuteAsync(null);

        Assert.Single(transport.WrittenPackets);
    }

    [Fact]
    public async Task ApplyingPreset_SetsActivePreset()
    {
        var (viewModel, _) = CreateViewModel();

        await viewModel.UphillApplyCommand.ExecuteAsync(null);

        Assert.Equal(ActivePreset.Uphill, viewModel.ActivePreset);
    }
}
