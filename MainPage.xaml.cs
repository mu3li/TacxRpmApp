using Microsoft.Extensions.DependencyInjection;
using TacxRpmApp.Core.ViewModels;

namespace TacxRpmApp;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private readonly IServiceProvider _services;

    public MainPage(MainViewModel viewModel, IServiceProvider services)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _services = services;
    }

    private async void OnConnectClicked(object sender, EventArgs e)
    {
        var permission = await Permissions.RequestAsync<Permissions.Bluetooth>();
        if (permission != PermissionStatus.Granted)
        {
            _viewModel.SetPermissionDenied();
            return;
        }

        var devicePage = _services.GetRequiredService<DevicePage>();
        await Navigation.PushAsync(devicePage);
        if (devicePage.ConnectionSucceeded)
        {
            _viewModel.SetConnected();
        }
    }

    private async void OnConfigClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(_services.GetRequiredService<ConfigPage>());
    }
}
