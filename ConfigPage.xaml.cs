using TacxRpmApp.Core.ViewModels;

namespace TacxRpmApp;

public partial class ConfigPage : ContentPage
{
    public ConfigPage(ConfigViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        viewModel.SaveCompleted += OnSaveCompleted;
    }

    private async void OnSaveCompleted(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
