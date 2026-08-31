using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TacxRpmApp.Core.Services;
using TacxRpmApp.Core.Settings;

namespace TacxRpmApp.Core.ViewModels;

public enum ActivePreset
{
    None,
    Uphill,
    Cruise,
    Downhill
}

public enum ConnectButtonState
{
    Idle,
    PermissionDenied,
    Connected
}

public partial class MainViewModel : ObservableObject
{
    private readonly TrainerService _trainer;
    private readonly RpmSettings _settings;

    [ObservableProperty]
    private ActivePreset activePreset = ActivePreset.None;

    [ObservableProperty]
    private string uphillValueText = "";

    [ObservableProperty]
    private string cruiseValueText = "";

    [ObservableProperty]
    private string downhillValueText = "";

    [ObservableProperty]
    private string statusText = "Desligado";

    [ObservableProperty]
    private string connectButtonText = "CONECTAR";

    [ObservableProperty]
    private ConnectButtonState connectButtonState = ConnectButtonState.Idle;

    public MainViewModel(TrainerService trainer, RpmSettings settings)
    {
        _trainer = trainer;
        _settings = settings;
        UpdatePresetLabels();
    }

    private int Increment => _settings.Increment;

    public void SetPermissionDenied()
    {
        ConnectButtonText = "PERMISSÃO NEGADA";
        ConnectButtonState = ConnectButtonState.PermissionDenied;
    }

    public void SetConnected()
    {
        ConnectButtonText = "CONECTADO";
        ConnectButtonState = ConnectButtonState.Connected;
    }

    [RelayCommand]
    private async Task UphillMinus()
    {
        _settings.Uphill -= Increment;
        UpdatePresetLabels();
        await UpdatePresetResistance(ActivePreset.Uphill, _settings.Uphill);
    }

    [RelayCommand]
    private async Task UphillPlus()
    {
        _settings.Uphill += Increment;
        UpdatePresetLabels();
        await UpdatePresetResistance(ActivePreset.Uphill, _settings.Uphill);
    }

    [RelayCommand]
    private async Task UphillApply()
    {
        await _trainer.SetResistanceAsync(_settings.Uphill);
        ActivePreset = ActivePreset.Uphill;
        StatusText = _trainer.LastConnectionStatus;
    }

    [RelayCommand]
    private async Task CruiseMinus()
    {
        _settings.Cruise -= Increment;
        UpdatePresetLabels();
        await UpdatePresetResistance(ActivePreset.Cruise, _settings.Cruise);
    }

    [RelayCommand]
    private async Task CruisePlus()
    {
        _settings.Cruise += Increment;
        UpdatePresetLabels();
        await UpdatePresetResistance(ActivePreset.Cruise, _settings.Cruise);
    }

    [RelayCommand]
    private async Task CruiseApply()
    {
        await _trainer.SetResistanceAsync(_settings.Cruise);
        ActivePreset = ActivePreset.Cruise;
        StatusText = _trainer.LastConnectionStatus;
    }

    [RelayCommand]
    private async Task DownhillMinus()
    {
        _settings.Downhill -= Increment;
        UpdatePresetLabels();
        await UpdatePresetResistance(ActivePreset.Downhill, _settings.Downhill);
    }

    [RelayCommand]
    private async Task DownhillPlus()
    {
        _settings.Downhill += Increment;
        UpdatePresetLabels();
        await UpdatePresetResistance(ActivePreset.Downhill, _settings.Downhill);
    }

    [RelayCommand]
    private async Task DownhillApply()
    {
        await _trainer.SetResistanceAsync(_settings.Downhill);
        ActivePreset = ActivePreset.Downhill;
        StatusText = _trainer.LastConnectionStatus;
    }

    private void UpdatePresetLabels()
    {
        UphillValueText = FormatResistance(_settings.Uphill);
        CruiseValueText = FormatResistance(_settings.Cruise);
        DownhillValueText = FormatResistance(_settings.Downhill);
    }

    private async Task UpdatePresetResistance(ActivePreset preset, int resistance)
    {
        if (ActivePreset == preset)
        {
            await _trainer.SetResistanceAsync(resistance);
        }
    }

    private static string FormatResistance(int value)
    {
        var clampedValue = Math.Clamp(value, 0, 200);
        return $"({clampedValue / 2.0:0.0}%)";
    }
}
