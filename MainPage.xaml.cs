namespace TacxRpmApp;

public partial class MainPage : ContentPage
{
    private readonly TacxNeoService _tacx;
    private readonly RpmSettings _settings;

    private enum ActivePreset
    {
        None,
        Uphill,
        Cruise,
        Downhill
    }

    private ActivePreset _activePreset;

    public MainPage(TacxNeoService tacx, RpmSettings settings)
    {
        InitializeComponent();
        _tacx = tacx;
        _settings = settings;
        SetActivePreset(ActivePreset.None);
    }

    private int Increment => _settings.Increment;

    // Ligação
    private async void OnConnectClicked(object sender, EventArgs e)
    {
        var permission = await Permissions.RequestAsync<Permissions.Bluetooth>();
        if (permission != PermissionStatus.Granted)
        {
            ConnectButton.Text = "PERMISSÃO NEGADA";
            ConnectButton.BackgroundColor = Color.FromArgb("#B9C0C8");
            return;
        }

        var devicePage = new DevicePage(_tacx);
        await Navigation.PushAsync(devicePage);
        if (devicePage.ConnectionSucceeded)
        {
            ConnectButton.Text = "CONECTADO";
            ConnectButton.BackgroundColor = Color.FromArgb("#39A96B");
        }
    }

    private async void OnConfigClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ConfigPage(_settings));
    }

    // Cruise
    private void OnCruiseMinus(object sender, EventArgs e)
    {
        _settings.Cruise = Math.Max(0, _settings.Cruise - Increment);
    }

    private void OnCruisePlus(object sender, EventArgs e)
    {
        _settings.Cruise += Increment;
    }

    private async void OnCruiseApply(object sender, EventArgs e)
    {
        await _tacx.SetResistanceAsync((ushort)_settings.Cruise);
        SetActivePreset(ActivePreset.Cruise);
    }

    // Uphill
    private void OnUphillMinus(object sender, EventArgs e)
    {
        _settings.Uphill = Math.Max(0, _settings.Uphill - Increment);
    }

    private void OnUphillPlus(object sender, EventArgs e)
    {
        _settings.Uphill += Increment;
    }

    private async void OnUphillApply(object sender, EventArgs e)
    {
        await _tacx.SetResistanceAsync((ushort)_settings.Uphill);
        SetActivePreset(ActivePreset.Uphill);
    }

    // Downhill
    private void OnDownhillMinus(object sender, EventArgs e)
    {
        _settings.Downhill = Math.Max(0, _settings.Downhill - Increment);
    }

    private void OnDownhillPlus(object sender, EventArgs e)
    {
        _settings.Downhill += Increment;
    }

    private async void OnDownhillApply(object sender, EventArgs e)
    {
        await _tacx.SetResistanceAsync((ushort)_settings.Downhill);
        SetActivePreset(ActivePreset.Downhill);
    }

    private void SetActivePreset(ActivePreset preset)
    {
        _activePreset = preset;
        UphillApplyButton.BackgroundColor = preset == ActivePreset.Uphill
            ? Color.FromArgb("#E84C3D")
            : Color.FromArgb("#B9C0C8");
        CruiseApplyButton.BackgroundColor = preset == ActivePreset.Cruise
            ? Color.FromArgb("#39A96B")
            : Color.FromArgb("#B9C0C8");
        DownhillApplyButton.BackgroundColor = preset == ActivePreset.Downhill
            ? Color.FromArgb("#2589D9")
            : Color.FromArgb("#B9C0C8");
    }
}
