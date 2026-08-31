using TacxRpmApp.Core.Ble;
using TacxRpmApp.Core.Protocol;

namespace TacxRpmApp.Core.Services;

/// <summary>
/// Scans for, connects to and reports readings from a Bluetooth Heart Rate strap
/// (service 0x180D, measurement characteristic 0x2A37) over an <see cref="IHeartRateTransport"/>.
/// </summary>
/// <example>
/// <code>
/// var monitor = new HeartRateMonitor(transport);
/// monitor.HeartRateReceived += (_, bpm) => MainThread.BeginInvokeOnMainThread(() =>
/// {
///     // Update your MAUI UI here, e.g. HeartRateLabel.Text = $"{bpm} BPM";
/// });
///
/// var devices = await monitor.ScanDevicesAsync();
/// if (devices.Count > 0)
/// {
///     var connected = await monitor.ConnectAsync(devices[0]);
/// }
///
/// // When finished:
/// monitor.Disconnect();
/// </code>
/// </example>
public sealed class HeartRateMonitor
{
    private static readonly TimeSpan ScanDuration = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(12);

    private readonly IHeartRateTransport _transport;

    public string LastConnectionStatus { get; private set; } = "";
    public int? HeartRate { get; private set; }

    public event EventHandler<int>? HeartRateReceived;

    public HeartRateMonitor(IHeartRateTransport transport)
    {
        _transport = transport;
        _transport.PacketReceived += OnPacketReceived;
    }

    public Task<IReadOnlyList<BleDeviceInfo>> ScanDevicesAsync(CancellationToken ct = default)
        => _transport.ScanAsync(ScanDuration, ct);

    public async Task<bool> ConnectAsync(BleDeviceInfo device, CancellationToken ct = default)
    {
        LastConnectionStatus = "A iniciar ligação...";

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(ConnectTimeout);

        bool connected;
        try
        {
            connected = await _transport.ConnectAsync(device, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            connected = false;
        }

        LastConnectionStatus = connected
            ? "Monitor cardíaco ligado."
            : "Ligação ao monitor cardíaco falhou.";
        return connected;
    }

    public void Disconnect()
    {
        _transport.Disconnect();
        LastConnectionStatus = "Desligado.";
    }

    private void OnPacketReceived(object? sender, byte[] packet)
    {
        var heartRate = HeartRateMeasurementParser.Parse(packet);
        if (heartRate is int bpm)
        {
            HeartRate = bpm;
            HeartRateReceived?.Invoke(this, bpm);
        }
    }
}
