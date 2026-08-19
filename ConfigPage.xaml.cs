namespace TacxRpmApp;

public partial class ConfigPage : ContentPage
{
    private readonly RpmSettings _settings;

    public ConfigPage(RpmSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        LoadValues();
    }

    private void LoadValues()
    {
        UphillEntry.Text = _settings.Uphill.ToString();
        CruiseEntry.Text = _settings.Cruise.ToString();
        DownhillEntry.Text = _settings.Downhill.ToString();
        IncrementEntry.Text = _settings.Increment.ToString();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (!int.TryParse(UphillEntry.Text, out var uphill)
            || !int.TryParse(CruiseEntry.Text, out var cruise)
            || !int.TryParse(DownhillEntry.Text, out var downhill)
            || !int.TryParse(IncrementEntry.Text, out var increment)
            || uphill < 0
            || cruise < 0
            || downhill < 0
            || uphill > 200
            || cruise > 200
            || downhill > 200
            || increment < 1)
        {
            StatusLabel.Text = "Use valores válidos.";
            return;
        }

        _settings.Uphill = uphill;
        _settings.Cruise = cruise;
        _settings.Downhill = downhill;
        _settings.Increment = increment;
        StatusLabel.Text = "Configuração guardada.";
        await Task.Delay(500);
        await Navigation.PopAsync();
    }
}
