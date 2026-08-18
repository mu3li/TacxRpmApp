using CommunityToolkit.Maui;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace TacxRpmApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit();

        builder.Services.AddSingleton<TacxNeoService>();
        builder.Services.AddSingleton<RpmSettings>();
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }
}
