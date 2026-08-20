Usage

I'd keep the UI-facing usage very simple:

var heartRateService = new HeartRateService();

heartRateService.HeartRateReceived += (_, bpm) =>
{
    MainThread.BeginInvokeOnMainThread(() =>
    {
        Console.WriteLine($"❤️ {bpm} BPM");

        // Update your MAUI UI here.
        // HeartRateLabel.Text = $"{bpm} BPM";
    });
};

var devices = await heartRateService.ScanDevicesAsync();

foreach (var device in devices)
{
    Console.WriteLine(
        $"{device.Name} - {device.Address}");
}

Then connect:

if (devices.Count > 0)
{
    var connected =
        await heartRateService.ConnectAsync(devices[0]);

    Console.WriteLine(
        connected
            ? "Heart rate monitor connected."
            : "Connection failed.");
}

And when you're finished:

heartRateService.Disconnect();
