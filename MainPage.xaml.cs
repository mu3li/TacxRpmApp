public partial class MainPage : ContentPage
{
    private readonly TacxNeoService _tacx;

    int cruise = 20;
    int uphill = 45;
    int downhill = 10;

    public MainPage(TacxNeoService tacx)
    {
        InitializeComponent();
        _tacx = tacx;
    }

    int Increment => int.TryParse(IncrementEntry.Text, out var inc) ? inc : 2;

    // Ligação
    private async void OnConnectClicked(object sender, EventArgs e)
    {
        StatusLabel.Text = "A ligar...";
        var ok = await _tacx.ConnectAsync();
        StatusLabel.Text = ok ? "Ligado ao Tacx" : "Falha na ligação";
    }

    // Cruise
    private void OnCruiseMinus(object sender, EventArgs e)
    {
        cruise = Math.Max(0, cruise - Increment);
        CruiseLabel.Text = $"{cruise} N";
    }

    private void OnCruisePlus(object sender, EventArgs e)
    {
        cruise += Increment;
        CruiseLabel.Text = $"{cruise} N";
    }

    private async void OnCruiseApply(object sender, EventArgs e)
    {
        await _tacx.SetResistanceAsync((ushort)cruise);
        StatusLabel.Text = $"Cruise ({cruise} N)";
    }

    // Uphill
    private void OnUphillMinus(object sender, EventArgs e)
    {
        uphill = Math.Max(0, uphill - Increment);
        UphillLabel.Text = $"{uphill} N";
    }

    private void OnUphillPlus(object sender, EventArgs e)
    {
        uphill += Increment;
        UphillLabel.Text = $"{uphill} N";
    }

    private async void OnUphillApply(object sender, EventArgs e)
    {
        await _tacx.SetResistanceAsync((ushort)uphill);
        StatusLabel.Text = $"Uphill ({uphill} N)";
    }

    // Downhill
    private void OnDownhillMinus(object sender, EventArgs e)
    {
        downhill = Math.Max(0, downhill - Increment);
        DownhillLabel.Text = $"{downhill} N";
    }

    private void OnDownhillPlus(object sender, EventArgs e)
    {
        downhill += Increment;
        DownhillLabel.Text = $"{downhill} N";
    }

    private async void OnDownhillApply(object sender, EventArgs e)
    {
        await _tacx.SetResistanceAsync((ushort)downhill);
        StatusLabel.Text = $"Downhill ({downhill} N)";
    }
}
