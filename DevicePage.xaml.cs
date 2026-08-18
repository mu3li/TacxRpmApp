namespace TacxRpmApp;

public partial class DevicePage : ContentPage
{
    private readonly TacxNeoService _tacx;

    public bool ConnectionSucceeded { get; private set; }

    public DevicePage(TacxNeoService tacx)
    {
        InitializeComponent();
        _tacx = tacx;
    }

    private async void OnScanClicked(object sender, EventArgs e)
    {
        DevicesList.ItemsSource = null;
        StatusLabel.Text = "A pesquisar...";

        var devices = await _tacx.ScanDevicesAsync();
        DevicesList.ItemsSource = devices;
        StatusLabel.Text = devices.Count == 0
            ? "Nenhum dispositivo encontrado."
            : $"{devices.Count} dispositivo(s) encontrado(s).";
    }

    private async void OnDeviceConnectClicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not BleDeviceInfo device)
        {
            return;
        }

        button.Text = "A LIGAR...";
        button.IsEnabled = false;
        var connected = await _tacx.ConnectAsync(device);
        if (connected)
        {
            ConnectionSucceeded = true;
            await Navigation.PopAsync();
            return;
        }

        button.Text = "TENTAR NOVAMENTE";
        button.IsEnabled = true;
        StatusLabel.Text = _tacx.LastConnectionStatus;
    }
}
