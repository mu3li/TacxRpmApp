using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TacxRpmApp.Core.Settings;

namespace TacxRpmApp.Core.ViewModels;

public partial class ConfigViewModel : ObservableObject
{
    private readonly RpmSettings _settings;

    [ObservableProperty]
    private string uphillText = "";

    [ObservableProperty]
    private string cruiseText = "";

    [ObservableProperty]
    private string downhillText = "";

    [ObservableProperty]
    private string incrementText = "";

    [ObservableProperty]
    private string statusText = "";

    public event EventHandler? SaveCompleted;

    public ConfigViewModel(RpmSettings settings)
    {
        _settings = settings;
        LoadValues();
    }

    private void LoadValues()
    {
        UphillText = _settings.Uphill.ToString();
        CruiseText = _settings.Cruise.ToString();
        DownhillText = _settings.Downhill.ToString();
        IncrementText = _settings.Increment.ToString();
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!int.TryParse(UphillText, out var uphill)
            || !int.TryParse(CruiseText, out var cruise)
            || !int.TryParse(DownhillText, out var downhill)
            || !int.TryParse(IncrementText, out var increment)
            || !IsValid(uphill, cruise, downhill, increment))
        {
            StatusText = "Use valores válidos.";
            return;
        }

        _settings.Uphill = uphill;
        _settings.Cruise = cruise;
        _settings.Downhill = downhill;
        _settings.Increment = increment;
        StatusText = "Configuração guardada.";
        await Task.Delay(500);
        SaveCompleted?.Invoke(this, EventArgs.Empty);
    }

    public static bool IsValid(int uphill, int cruise, int downhill, int increment)
        => uphill is >= 0 and <= 200
        && cruise is >= 0 and <= 200
        && downhill is >= 0 and <= 200
        && increment >= 1;
}
