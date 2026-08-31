using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TacxRpmApp.Core.Ble;
using TacxRpmApp.Core.Services;

namespace TacxRpmApp.Core.ViewModels;

public partial class DeviceViewModel : ObservableObject
{
    private readonly TrainerService _trainer;

    public ObservableCollection<BleDeviceInfo> Devices { get; } = new();

    [ObservableProperty]
    private string statusText = "Toque em pesquisar para procurar dispositivos BLE.";

    [ObservableProperty]
    private bool isBusy;

    public bool ConnectionSucceeded { get; private set; }

    public event EventHandler? ConnectionEstablished;

    public DeviceViewModel(TrainerService trainer)
    {
        _trainer = trainer;
    }

    [RelayCommand]
    private async Task Scan()
    {
        Devices.Clear();
        StatusText = "A pesquisar...";

        var found = await _trainer.ScanDevicesAsync();
        foreach (var device in found)
        {
            Devices.Add(device);
        }

        StatusText = found.Count == 0
            ? "Nenhum dispositivo encontrado."
            : $"{found.Count} dispositivo(s) encontrado(s).";
    }

    [RelayCommand]
    private async Task Connect(BleDeviceInfo device)
    {
        IsBusy = true;
        var connected = await _trainer.ConnectAsync(device);
        IsBusy = false;

        if (connected)
        {
            ConnectionSucceeded = true;
            ConnectionEstablished?.Invoke(this, EventArgs.Empty);
            return;
        }

        StatusText = _trainer.LastConnectionStatus;
    }
}
