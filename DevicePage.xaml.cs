using TacxRpmApp.Core.ViewModels;

namespace TacxRpmApp;

public partial class DevicePage : ContentPage
{
    private readonly DeviceViewModel _viewModel;

    public bool ConnectionSucceeded => _viewModel.ConnectionSucceeded;

    public DevicePage(DeviceViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _viewModel.ConnectionEstablished += OnConnectionEstablished;
    }

    private async void OnConnectionEstablished(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
