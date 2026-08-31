using CommunityToolkit.Maui;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using TacxRpmApp.Core.Ble;
using TacxRpmApp.Core.Services;
using TacxRpmApp.Core.Settings;
using TacxRpmApp.Core.ViewModels;

namespace TacxRpmApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit();

        builder.Services.AddSingleton<IPreferenceStore, MauiPreferenceStore>();
        builder.Services.AddSingleton<RpmSettings>();

        builder.Services.AddSingleton<ITrainerTransport, AndroidTrainerTransport>();
        builder.Services.AddSingleton<IHeartRateTransport, AndroidHeartRateTransport>();
        builder.Services.AddSingleton<TrainerService>();
        builder.Services.AddSingleton<HeartRateMonitor>();

        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddTransient<DeviceViewModel>();
        builder.Services.AddTransient<ConfigViewModel>();

        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddTransient<DevicePage>();
        builder.Services.AddTransient<ConfigPage>();

        return builder.Build();
    }
}

